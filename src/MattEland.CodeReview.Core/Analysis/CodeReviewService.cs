using System.Text.Json;
using MattEland.CodeReview.Core.Configuration;
using MattEland.CodeReview.Core.Git;
using MattEland.CodeReview.Core.Models;
using MattEland.CodeReview.Core.Prompts;
using MattEland.CodeReview.Core.Rules;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MattEland.CodeReview.Core.Analysis;

/// <summary>
/// Implementation of <see cref="ICodeReviewService"/>.
/// </summary>
public sealed class CodeReviewService : ICodeReviewService
{
    private readonly IGitService _gitService;
    private readonly IRuleProvider _ruleProvider;
    private readonly IPromptLoader _promptLoader;
    private readonly ChatClientFactory _chatClientFactory;
    private readonly CodeReviewOptions _options;
    private readonly ILogger<CodeReviewService> _logger;

    public CodeReviewService(
        IGitService gitService,
        IRuleProvider ruleProvider,
        IPromptLoader promptLoader,
        ChatClientFactory chatClientFactory,
        IOptions<CodeReviewOptions> options,
        ILogger<CodeReviewService> logger)
    {
        _gitService = gitService;
        _ruleProvider = ruleProvider;
        _promptLoader = promptLoader;
        _chatClientFactory = chatClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReviewResult> AnalyzeDiffAsync(
        string repositoryPath,
        string baseBranch = "main",
        CancellationToken cancellationToken = default)
    {
        var diff = await _gitService.GetDiffFromBaseAsync(repositoryPath, baseBranch, cancellationToken);
        return await AnalyzeDiffAsync(diff, progressReporter: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReviewResult> AnalyzeFilesAsync(
        string repositoryPath,
        IEnumerable<string> filePaths,
        string baseBranch = "main",
        CancellationToken cancellationToken = default)
    {
        var diff = await _gitService.GetDiffForFilesAsync(repositoryPath, filePaths, baseBranch, cancellationToken);
        return await AnalyzeDiffAsync(diff, progressReporter: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReviewResult> AnalyzeDiffAsync(
        GitDiff diff,
        IAnalysisProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        var reviewId = Guid.NewGuid().ToString("N")[..8];
        var startedAt = DateTimeOffset.UtcNow;
        var allIssues = new List<Issue>();
        var appliedRules = new List<Rule>();
        var errors = new List<string>();

        _logger.LogInformation("Starting review {ReviewId} with {FileCount} files",
            reviewId, diff.Files.Count);

        try
        {
            // Group files by language
            var filesByLanguage = diff.Files
                .Where(f => !string.IsNullOrEmpty(f.Language))
                .GroupBy(f => f.Language!)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Calculate total work items for progress tracking
            var totalWorkItems = filesByLanguage.Values
                .Sum(files => files.Count * 
                    _ruleProvider.GetRulesByLanguage(files.First().Language!)
                        .Count(r => r.Enabled && !string.IsNullOrEmpty(r.PromptContent)));
            var currentWorkItem = 0;

            foreach (var (language, files) in filesByLanguage)
            {
                var rules = _ruleProvider.GetRulesByLanguage(language)
                    .Where(r => r.Enabled && !string.IsNullOrEmpty(r.PromptContent))
                    .ToList();

                _logger.LogDebug("Analyzing {FileCount} {Language} files with {RuleCount} rules",
                    files.Count, language, rules.Count);

                // Add rules to the list of applied rules
                appliedRules.AddRange(rules);

                var fileIndex = 0;
                foreach (var file in files)
                {
                    fileIndex++;
                    var ruleIndex = 0;

                    foreach (var rule in rules)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        currentWorkItem++;
                        ruleIndex++;
                        
                        // Report progress
                        progressReporter?.ReportProgress(
                            fileIndex, files.Count, 
                            ruleIndex, rules.Count,
                            file.Path, rule.Id);
                        
                        var (issues, error) = await AnalyzeFileWithRuleAsync(
                            file, rule, progressReporter, cancellationToken);
                        allIssues.AddRange(issues);
                        
                        if (!string.IsNullOrEmpty(error))
                        {
                            errors.Add(error);
                        }
                    }
                }
            }

            // If we have model errors, mark as failed
            var modelErrors = errors.Where(e => e.Contains("model", StringComparison.OrdinalIgnoreCase) || 
                                                e.Contains("not found", StringComparison.OrdinalIgnoreCase)).ToList();
            
            var errorMessage = modelErrors.Count > 0 
                ? string.Join("; ", modelErrors.Distinct().Take(3))
                : errors.Count > 0 
                    ? $"{errors.Count} error(s) occurred during analysis. Check logs for details."
                    : null;

            return new ReviewResult
            {
                Id = reviewId,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Diff = diff,
                Issues = allIssues,
                AppliedRules = appliedRules,
                IsSuccess = modelErrors.Count == 0,
                ErrorMessage = errorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Review {ReviewId} failed", reviewId);
            
            return new ReviewResult
            {
                Id = reviewId,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Diff = diff,
                Issues = allIssues,
                AppliedRules = appliedRules,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<(IEnumerable<Issue> Issues, string? Error)> AnalyzeFileWithRuleAsync(
        FileChange file,
        Rule rule,
        IAnalysisProgressReporter? progressReporter,
        CancellationToken cancellationToken)
    {
        try
        {
            // Truncate diff if too large
            var diffContent = file.DiffContent;
            if (diffContent.Length > _options.Git.MaxDiffSizePerFile)
            {
                _logger.LogWarning("Truncating diff for {Path} from {Original} to {Max} characters",
                    file.Path, diffContent.Length, _options.Git.MaxDiffSizePerFile);
                diffContent = diffContent[.._options.Git.MaxDiffSizePerFile] + "\n... (truncated)";
            }

            var context = new PromptContext
            {
                Diff = diffContent,
                FilePath = file.Path,
                Language = file.Language
            };

            var renderedPrompt = _promptLoader.RenderPrompt(rule, context);

            _logger.LogDebug("Analyzing {Path} with rule {RuleId}", file.Path, rule.Id);

            // Report LLM request
            var requestPreview = TruncateForDisplay(renderedPrompt, 500);
            progressReporter?.ReportLlmRequest(file.Path, rule.Id, requestPreview);

            // Get a fresh client with the latest configuration
            var chatClient = _chatClientFactory.CreateClient();
            var response = await chatClient.GetResponseAsync(
                [
                    new ChatMessage(ChatRole.System, GetSystemPrompt()),
                    new ChatMessage(ChatRole.User, renderedPrompt)
                ],
                cancellationToken: cancellationToken);

            var responseText = response.Text ?? string.Empty;
            
            // Report LLM response
            var responsePreview = TruncateForDisplay(responseText, 500);
            progressReporter?.ReportLlmResponse(file.Path, rule.Id, responsePreview);
            
            var issues = ParseIssuesFromResponse(responseText, file, rule, progressReporter);
            progressReporter?.ReportIssues(file.Path, rule.Id, issues);
            
            return (issues, null);
        }
        catch (Exception ex)
        {
            var errorMessage = ex.Message;
            
            // Check for model-specific errors
            if (errorMessage.Contains("model", StringComparison.OrdinalIgnoreCase) && 
                errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(ex, "Model error while analyzing {Path} with rule {RuleId}: {Error}", 
                    file.Path, rule.Id, errorMessage);
                return ([], errorMessage);
            }
            
            _logger.LogWarning(ex, "Failed to analyze {Path} with rule {RuleId}", file.Path, rule.Id);
            return ([], null);
        }
    }

    private static string GetSystemPrompt() => """
        You are an expert code reviewer analyzing git diffs for potential issues.
        Your specialty is finding issues that static analysis tools miss: logic errors,
        subtle security vulnerabilities, architectural mistakes, and semantic problems.

        CRITICAL GUIDELINES:
        1. Only report issues you are CONFIDENT about (confidence >= 0.7)
        2. If unsure, return an empty array []
        3. Consider context: code using configuration/DI patterns is NOT hardcoding secrets
        4. Focus on NEW or CHANGED code (lines with + prefix in the diff)
        5. Do NOT report style issues, that's what linters are for
        
        LINE NUMBER INSTRUCTIONS:
        The diff contains hunk headers like @@ -10,5 +12,7 @@ where +12 is the starting
        line number in the NEW file. Count added lines (+) and context lines (space prefix)
        from there. Return the ACTUAL SOURCE FILE line number, NOT the position in diff text.
        
        Example: If hunk starts @@ -1,3 +1,5 @@ and issue is on 3rd line with '+', that's line 3.
        
        When you find issues, respond with a JSON array of objects with these fields:
        - line: (number or null) The actual SOURCE FILE line number
        - message: (string) A clear description of the issue
        - suggestion: (string or null) How to fix the issue
        - severity: (string) One of: info, warning, error, critical
        - confidence: (number) Your confidence from 0.0 to 1.0 - be conservative
        
        If you find no issues, respond with an empty array: []
        
        IMPORTANT: Only output valid JSON. Do not include any text before or after the JSON array.
        """;

    private IEnumerable<Issue> ParseIssuesFromResponse(
        string response, 
        FileChange file, 
        Rule rule, 
        IAnalysisProgressReporter? progressReporter)
    {
        try
        {
            // Try to extract JSON from the response (handle markdown code blocks)
            var jsonContent = ExtractJson(response);
            
            if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent == "[]")
            {
                return [];
            }

            var issueData = JsonSerializer.Deserialize<List<LlmIssue>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (issueData == null)
                return [];

            // Filter out low-confidence issues (below 0.6 threshold)
            const double minConfidence = 0.6;
            var filteredIssues = issueData.Where(d => d.Confidence == null || d.Confidence >= minConfidence);

            return filteredIssues.Select((data, index) => new Issue
            {
                Id = $"{rule.Id}-{file.Path.GetHashCode():X8}-{index}",
                RuleId = rule.Id,
                FilePath = file.Path,
                StartLine = data.Line,
                Severity = ParseSeverity(data.Severity) ?? rule.DefaultSeverity,
                Message = data.Message ?? "Issue detected",
                Suggestion = data.Suggestion,
                Confidence = data.Confidence,
                CodeSnippet = data.Line.HasValue 
                    ? DiffLineMapper.ExtractCodeSnippet(file.NewContent, data.Line.Value, contextLines: 3) 
                    : null
            });
        }
        catch (JsonException ex)
        {
            var errorMessage = $"JSON parsing error: {ex.Message}";
            var responsePreview = TruncateForDisplay(response, 1000);
            
            _logger.LogWarning(ex, "Failed to parse LLM response as JSON: {Response}", 
                response.Length > 200 ? response[..200] + "..." : response);
            
            // Report JSON parsing error
            progressReporter?.ReportJsonParsingError(file.Path, rule.Id, errorMessage, responsePreview);
            
            return [];
        }
    }

    private static string TruncateForDisplay(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text[..maxLength] + $"\n... (truncated, {text.Length - maxLength} more characters)";
    }

    private static string ExtractJson(string response)
    {
        // Handle markdown code blocks
        var trimmed = response.Trim();
        
        if (trimmed.StartsWith("```json"))
        {
            var endIndex = trimmed.LastIndexOf("```");
            if (endIndex > 7)
            {
                return trimmed[7..endIndex].Trim();
            }
        }
        
        if (trimmed.StartsWith("```"))
        {
            var endIndex = trimmed.LastIndexOf("```");
            if (endIndex > 3)
            {
                var start = trimmed.IndexOf('\n') + 1;
                return trimmed[start..endIndex].Trim();
            }
        }

        // Try to find JSON array in the response
        var arrayStart = trimmed.IndexOf('[');
        var arrayEnd = trimmed.LastIndexOf(']');
        
        if (arrayStart >= 0 && arrayEnd > arrayStart)
        {
            return trimmed[arrayStart..(arrayEnd + 1)];
        }

        return trimmed;
    }

    private static Severity? ParseSeverity(string? severity) => severity?.ToLowerInvariant() switch
    {
        "info" => Severity.Info,
        "warning" => Severity.Warning,
        "error" => Severity.Error,
        "critical" => Severity.Critical,
        _ => null
    };

    private sealed class LlmIssue
    {
        public int? Line { get; set; }
        public string? Message { get; set; }
        public string? Suggestion { get; set; }
        public string? Severity { get; set; }
        public double? Confidence { get; set; }
    }
}
