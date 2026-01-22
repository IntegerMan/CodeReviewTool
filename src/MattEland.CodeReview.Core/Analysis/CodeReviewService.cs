using System.Text.Json;
using MattEland.CodeReview.Core.Configuration;
using MattEland.CodeReview.Core.Git;
using MattEland.CodeReview.Core.Models;
using MattEland.CodeReview.Core.Profiles;
using MattEland.CodeReview.Core.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MattEland.CodeReview.Core.Analysis;

/// <summary>
/// Implementation of <see cref="ICodeReviewService"/> using review profiles.
/// </summary>
public sealed class CodeReviewService : ICodeReviewService
{
    private readonly IGitService _gitService;
    private readonly IProfileProvider _profileProvider;
    private readonly IProfileLoader _profileLoader;
    private readonly ChatClientFactory _chatClientFactory;
    private readonly CodeReviewOptions _options;
    private readonly ILogger<CodeReviewService> _logger;

    /// <summary>
    /// Maximum number of files to include in a single batch for analysis.
    /// </summary>
    private const int MaxFilesPerBatch = 10;

    /// <summary>
    /// Maximum total diff size (in characters) per batch to avoid token limits.
    /// </summary>
    private const int MaxBatchDiffSize = 50000;

    public CodeReviewService(
        IGitService gitService,
        IProfileProvider profileProvider,
        IProfileLoader profileLoader,
        ChatClientFactory chatClientFactory,
        IOptions<CodeReviewOptions> options,
        ILogger<CodeReviewService> logger)
    {
        _gitService = gitService;
        _profileProvider = profileProvider;
        _profileLoader = profileLoader;
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
    public Task<ReviewResult> AnalyzeDiffAsync(
        GitDiff diff,
        IAnalysisProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        return AnalyzeDiffAsync(diff, selectedProfileIds: null, progressReporter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReviewResult> AnalyzeDiffAsync(
        GitDiff diff,
        IEnumerable<string>? selectedProfileIds,
        IAnalysisProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        var reviewId = Guid.NewGuid().ToString("N")[..8];
        var startedAt = DateTimeOffset.UtcNow;
        var allIssues = new List<Issue>();
        var appliedProfiles = new List<ReviewProfile>();
        var errors = new List<string>();
        var selectedProfileIdSet = selectedProfileIds?.ToHashSet();

        _logger.LogInformation("Starting review {ReviewId} with {FileCount} files, {ProfileFilter}",
            reviewId, diff.Files.Count, 
            selectedProfileIdSet != null ? $"{selectedProfileIdSet.Count} selected profiles" : "all enabled profiles");

        try
        {
            // Get profiles to apply
            var profiles = GetFilteredProfiles(selectedProfileIdSet).ToList();
            appliedProfiles.AddRange(profiles);

            if (profiles.Count == 0)
            {
                _logger.LogWarning("No profiles available for analysis");
                return CreateResult(reviewId, startedAt, diff, [], appliedProfiles, true, "No profiles available");
            }

            // Filter out deletions and irrelevant files (e.g., .gitignore, IDE configs)
            var filesToAnalyze = diff.Files
                .Where(f => f.ChangeType != FileChangeType.Deleted)
                .Where(f => !IsIrrelevantFile(f.Path))
                .ToList();

            // Create intelligent batches of files
            var batches = CreateIntelligentBatches(filesToAnalyze);
            _logger.LogInformation("Created {BatchCount} batches from {FileCount} files (filtered from {TotalFiles})", 
                batches.Count, filesToAnalyze.Count, diff.Files.Count);

            var batchIndex = 0;
            foreach (var (batch, groupingReason) in batches)
            {
                batchIndex++;
                
                // Report batch start with file list and grouping reason
                progressReporter?.ReportBatchStart(batchIndex, batches.Count, batch, groupingReason);
                
                foreach (var profile in profiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    progressReporter?.ReportProgress(
                        batchIndex, batches.Count,
                        profiles.IndexOf(profile) + 1, profiles.Count,
                        batch.Count == 1 ? batch[0].Path : $"Batch {batchIndex} ({batch.Count} files)", profile.Id);

                    var (issues, error) = await AnalyzeBatchWithProfileAsync(
                        batch, profile, progressReporter, cancellationToken);
                    
                    allIssues.AddRange(issues);

                    if (!string.IsNullOrEmpty(error))
                    {
                        errors.Add(error);
                    }
                }
            }


            var modelErrors = errors.Where(e => e.Contains("model", StringComparison.OrdinalIgnoreCase) || 
                                                e.Contains("not found", StringComparison.OrdinalIgnoreCase)).ToList();
            
            var errorMessage = modelErrors.Count > 0 
                ? string.Join("; ", modelErrors.Distinct().Take(3))
                : errors.Count > 0 
                    ? $"{errors.Count} error(s) occurred during analysis. Check logs for details."
                    : null;

            return CreateResult(reviewId, startedAt, diff, allIssues, appliedProfiles, modelErrors.Count == 0, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Review {ReviewId} failed", reviewId);
            return CreateResult(reviewId, startedAt, diff, allIssues, appliedProfiles, false, ex.Message);
        }
    }

    /// <summary>
    /// Creates intelligent batches of files for analysis, grouping semantically related files together.
    /// First groups by entity/feature name (e.g., UserController + UserService), then by directory.
    /// </summary>
    internal List<(List<FileChange> Files, string Reason)> CreateIntelligentBatches(List<FileChange> files)
    {
        var batches = new List<(List<FileChange>, string)>();
        var unassigned = new HashSet<FileChange>(files);
        
        // Phase 1: Group by entity/feature relationships (e.g., UserController + UserService + UserRepository)
        var byEntity = files
            .Select(f => (File: f, Entity: ExtractEntityName(f.Path)))
            .Where(x => x.Entity != null)
            .GroupBy(x => x.Entity!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1) // Only group if multiple related files
            .OrderByDescending(g => g.Count());

        foreach (var entityGroup in byEntity)
        {
            var entityFiles = entityGroup.Select(x => x.File).ToList();
            
            // Only add if all files are still unassigned and we're under size limits
            if (entityFiles.All(f => unassigned.Contains(f)) && entityFiles.Count <= MaxFilesPerBatch)
            {
                var totalSize = entityFiles.Sum(f => f.DiffContent?.Length ?? 0);
                if (totalSize <= MaxBatchDiffSize)
                {
                    // Describe the layer types found for context
                    var layers = entityFiles
                        .Select(f => GetLayerType(f.Path))
                        .Where(l => l != null)
                        .Distinct()
                        .OrderBy(l => l)
                        .ToList();
                    
                    var reason = layers.Count > 0
                        ? $"Related '{entityGroup.Key}' files ({string.Join(", ", layers)})"
                        : $"Related '{entityGroup.Key}' files";
                    
                    batches.Add((entityFiles, reason));
                    foreach (var f in entityFiles) unassigned.Remove(f);
                }
            }
        }

        // Phase 2: Group remaining files by directory
        var byDirectory = unassigned
            .GroupBy(f => Path.GetDirectoryName(f.Path) ?? "")
            .OrderBy(g => g.Key);

        foreach (var dirGroup in byDirectory)
        {
            var dirBatch = new List<FileChange>();
            var batchSize = 0;

            foreach (var file in dirGroup)
            {
                var fileSize = file.DiffContent?.Length ?? 0;
                
                if (dirBatch.Count >= MaxFilesPerBatch || 
                    (batchSize + fileSize > MaxBatchDiffSize && dirBatch.Count > 0))
                {
                    var reason = string.IsNullOrEmpty(dirGroup.Key) 
                        ? "Files from root directory"
                        : $"Files from {dirGroup.Key}";
                    batches.Add((dirBatch, reason));
                    dirBatch = [];
                    batchSize = 0;
                }
                
                dirBatch.Add(file);
                batchSize += fileSize;
            }

            if (dirBatch.Count > 0)
            {
                var reason = string.IsNullOrEmpty(dirGroup.Key)
                    ? "Files from root directory"
                    : $"Files from {dirGroup.Key}";
                batches.Add((dirBatch, reason));
            }
        }

        return batches;
    }

    /// <summary>
    /// Extracts an entity/feature name from a file path by stripping common suffixes.
    /// For example, "UserController.cs" -> "User", "OrderService.cs" -> "Order".
    /// </summary>
    internal static string? ExtractEntityName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        
        string[] suffixes = ["Controller", "Service", "Repository", "Handler", 
                             "Manager", "Provider", "Factory", "Validator", 
                             "Model", "Entity", "Dto", "ViewModel", "Command",
                             "Query", "Endpoint", "Consumer", "Worker", "Job"];
        
        foreach (var suffix in suffixes)
        {
            if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && fileName.Length > suffix.Length)
                return fileName[..^suffix.Length];
        }
        
        return null;
    }

    /// <summary>
    /// Gets a human-readable layer type from a file path.
    /// </summary>
    internal static string? GetLayerType(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (fileName.Contains("Controller", StringComparison.OrdinalIgnoreCase)) return "Controller";
        if (fileName.Contains("Service", StringComparison.OrdinalIgnoreCase)) return "Service";
        if (fileName.Contains("Repository", StringComparison.OrdinalIgnoreCase)) return "Repository";
        if (fileName.Contains("Handler", StringComparison.OrdinalIgnoreCase)) return "Handler";
        if (fileName.Contains("Model", StringComparison.OrdinalIgnoreCase)) return "Model";
        if (fileName.Contains("ViewModel", StringComparison.OrdinalIgnoreCase)) return "ViewModel";
        if (fileName.Contains("Endpoint", StringComparison.OrdinalIgnoreCase)) return "Endpoint";
        if (fileName.Contains("Consumer", StringComparison.OrdinalIgnoreCase)) return "Consumer";
        return null;
    }


    private IEnumerable<ReviewProfile> GetFilteredProfiles(HashSet<string>? selectedProfileIds)
    {
        var enabledProfiles = _profileProvider.GetEnabledProfiles()
            .Where(p => !string.IsNullOrEmpty(p.PromptContent));
        
        if (selectedProfileIds == null)
        {
            return enabledProfiles;
        }
        
        return enabledProfiles.Where(p => selectedProfileIds.Contains(p.Id));
    }

    private async Task<(IEnumerable<Issue> Issues, string? Error)> AnalyzeBatchWithProfileAsync(
        List<FileChange> batch,
        ReviewProfile profile,
        IAnalysisProgressReporter? progressReporter,
        CancellationToken cancellationToken)
    {
        try
        {
            // Build combined diff with file summaries for context
            var combinedDiff = BuildBatchDiff(batch);

            var context = new PromptContext
            {
                Diff = combinedDiff,
                FilePath = batch.Count == 1 ? batch[0].Path : $"{batch.Count} files",
                Language = batch.FirstOrDefault()?.Language
            };

            var renderedPrompt = _profileLoader.RenderPrompt(profile, context);

            _logger.LogDebug("Analyzing batch of {FileCount} files with profile {ProfileId}", batch.Count, profile.Id);

            // Report LLM request
            var requestPreview = TruncateForDisplay(renderedPrompt, 500);
            progressReporter?.ReportLlmRequest(context.FilePath ?? "batch", profile.Id, requestPreview);

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
            progressReporter?.ReportLlmResponse(context.FilePath ?? "batch", profile.Id, responsePreview);
            
            var issues = ParseIssuesFromResponse(responseText, batch, profile, progressReporter);
            progressReporter?.ReportIssues(context.FilePath ?? "batch", profile.Id, issues);
            
            return (issues, null);
        }
        catch (Exception ex)
        {
            var errorMessage = ex.Message;
            
            if (errorMessage.Contains("model", StringComparison.OrdinalIgnoreCase) && 
                errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(ex, "Model error while analyzing batch with profile {ProfileId}: {Error}", 
                    profile.Id, errorMessage);
                return ([], errorMessage);
            }
            
            _logger.LogWarning(ex, "Failed to analyze batch with profile {ProfileId}", profile.Id);
            return ([], null);
        }
    }

    /// <summary>
    /// Builds a combined diff for a batch of files with summaries for context.
    /// </summary>
    private string BuildBatchDiff(List<FileChange> batch)
    {
        var builder = new System.Text.StringBuilder();

        foreach (var file in batch)
        {
            builder.AppendLine($"=== File: {file.Path} ===");
            builder.AppendLine($"Change Type: {file.ChangeType}, +{file.LinesAdded}/-{file.LinesDeleted} lines");
            builder.AppendLine();
            
            var diffContent = file.DiffContent;
            if (!string.IsNullOrEmpty(diffContent))
            {
                // Truncate very large diffs per file
                if (diffContent.Length > _options.Git.MaxDiffSizePerFile)
                {
                    diffContent = diffContent[.._options.Git.MaxDiffSizePerFile] + "\n... (truncated)";
                }
                builder.AppendLine(diffContent);
            }
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string GetSystemPrompt() => """
        You are an expert code reviewer analyzing code changes holistically.
        Your specialty is finding issues that static analysis tools miss: confusing code,
        unclear logic, hidden complexity, and maintainability problems.

        CRITICAL GUIDELINES:
        1. Focus on code clarity and maintainability
        2. If unsure, return an empty array []
        3. Focus on NEW or CHANGED code (lines with + prefix in the diff)
        4. Do NOT report style issues, that's what linters are for
        
        LINE NUMBER INSTRUCTIONS:
        The diff contains hunk headers like @@ -10,5 +12,7 @@ where +12 is the starting 
        line number in the NEW file.
        - Calculate the correct line number for every issue.
        - Return the ACTUAL FILE line number.
        - Include both startLine and endLine for the issue range.
        
        When you find issues, respond with a JSON array of objects with these fields:
        - file: (string) MANDATORY. The file path where the issue was found.
        - startLine: (number) MANDATORY. The starting line number of the issue.
        - endLine: (number) MANDATORY. The ending line number of the issue.
        - comments: (string) A concise description of the issue.
        - reasoning: (string) Detailed explanation of why this is problematic.
        - severity: (string) One of: info, warning, error, critical
        
        If you find no issues, respond with an empty array: []
        
        IMPORTANT: Only output valid JSON. Do not include any text before or after the JSON array.
        """;

    private IEnumerable<Issue> ParseIssuesFromResponse(
        string response, 
        List<FileChange> batch,
        ReviewProfile profile, 
        IAnalysisProgressReporter? progressReporter)
    {
        try
        {
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

            return issueData.Select((data, index) => 
            {
                // Find the matching file from the batch with improved matching
                var matchingFile = FindMatchingFile(batch, data.File);

                var filePath = matchingFile?.Path ?? data.File ?? "unknown";

                var startLine = data.StartLine ?? data.Line;
                var endLine = data.EndLine ?? startLine;

                // Extract code snippet with fallback to diff content
                string? codeSnippet = null;
                string? codeSnippetError = null;
                
                if (!startLine.HasValue)
                {
                    codeSnippetError = "No line number provided by reviewer";
                }
                else if (matchingFile == null)
                {
                    codeSnippetError = $"Could not match file path: {data.File}";
                }
                else
                {
                    // Try from full file content first
                    codeSnippet = DiffLineMapper.ExtractCodeSnippet(matchingFile.NewContent, startLine.Value, contextLines: 2);
                    
                    // Fallback to extracting from diff content if file content unavailable
                    if (codeSnippet == null && !string.IsNullOrEmpty(matchingFile.DiffContent))
                    {
                        codeSnippet = DiffLineMapper.ExtractSnippetFromDiff(matchingFile.DiffContent, startLine.Value, contextLines: 2);
                    }

                    if (codeSnippet == null)
                    {
                        codeSnippetError = string.IsNullOrEmpty(matchingFile.NewContent) 
                            ? "File content not available; line not found in diff"
                            : $"Line {startLine.Value} not found in file";
                    }
                }

                return new Issue
                {
                    Id = $"{profile.Id}-{filePath.GetHashCode():X8}-{index}",
                    ProfileId = profile.Id,
                    FilePath = filePath,
                    StartLine = startLine,
                    EndLine = endLine,
                    Severity = ParseSeverity(data.Severity) ?? Severity.Warning,
                    Message = data.Comments ?? data.Message ?? "Issue detected",
                    Reasoning = data.Reasoning,
                    CodeSnippet = codeSnippet,
                    CodeSnippetError = codeSnippetError
                };
            });
        }
        catch (JsonException ex)
        {
            var errorMessage = $"JSON parsing error: {ex.Message}";
            var responsePreview = TruncateForDisplay(response, 1000);
            
            _logger.LogWarning(ex, "Failed to parse LLM response as JSON: {Response}", 
                response.Length > 200 ? response[..200] + "..." : response);
            
            progressReporter?.ReportJsonParsingError("batch", profile.Id, errorMessage, responsePreview);
            
            return [];
        }
    }

    /// <summary>
    /// Finds a matching file from the batch with improved path matching.
    /// </summary>
    private static FileChange? FindMatchingFile(List<FileChange> batch, string? llmFilePath)
    {
        if (string.IsNullOrEmpty(llmFilePath))
            return batch.Count == 1 ? batch[0] : null;

        // Normalize the LLM path for comparison
        var normalizedLlmPath = NormalizePath(llmFilePath);
        var llmFileName = Path.GetFileName(normalizedLlmPath);

        // Try exact match first (with normalization)
        var exactMatch = batch.FirstOrDefault(f => 
            NormalizePath(f.Path).Equals(normalizedLlmPath, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch;

        // Try ends-with match (for partial paths)
        var endsWithMatch = batch.FirstOrDefault(f => 
            NormalizePath(f.Path).EndsWith(normalizedLlmPath, StringComparison.OrdinalIgnoreCase) ||
            normalizedLlmPath.EndsWith(NormalizePath(f.Path), StringComparison.OrdinalIgnoreCase));
        if (endsWithMatch != null)
            return endsWithMatch;

        // Fallback to filename-only match
        var fileNameMatch = batch.FirstOrDefault(f => 
            Path.GetFileName(f.Path).Equals(llmFileName, StringComparison.OrdinalIgnoreCase));
        if (fileNameMatch != null)
            return fileNameMatch;

        // If single file in batch, use it as default
        return batch.Count == 1 ? batch[0] : null;
    }

    /// <summary>
    /// Normalizes path separators for consistent comparison.
    /// </summary>
    private static string NormalizePath(string path) => 
        path.Replace('\\', '/').TrimStart('/');


    private static ReviewResult CreateResult(
        string reviewId,
        DateTimeOffset startedAt,
        GitDiff diff,
        List<Issue> issues,
        List<ReviewProfile> profiles,
        bool isSuccess,
        string? errorMessage)
    {
        return new ReviewResult
        {
            Id = reviewId,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            Diff = diff,
            Issues = issues,
            AppliedProfiles = profiles,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage
        };
    }

    private static string TruncateForDisplay(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text[..maxLength] + $"\n... (truncated, {text.Length - maxLength} more characters)";
    }

    private static string ExtractJson(string response)
    {
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

        var arrayStart = trimmed.IndexOf('[');
        var arrayEnd = trimmed.LastIndexOf(']');
        
        if (arrayStart >= 0 && arrayEnd > arrayStart)
        {
            return trimmed[arrayStart..(arrayEnd + 1)];
        }

        return trimmed;
    }

    private static bool IsIrrelevantFile(string path)
    {
        var normalizedPath = path.Replace('\\', '/').ToLowerInvariant();
        
        // Common files/directories to ignore
        return normalizedPath == ".gitignore" ||
               normalizedPath.StartsWith(".vscode/") ||
               normalizedPath.Contains("/.vscode/") ||
               normalizedPath.StartsWith(".cursor/") ||
               normalizedPath.Contains("/.cursor/") ||
               normalizedPath == ".cursorrules" ||
               normalizedPath == "package-lock.json" ||
               normalizedPath == "yarn.lock" ||
               normalizedPath == "pnpm-lock.yaml";
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
        public string? File { get; set; }
        public int? Line { get; set; }
        public int? StartLine { get; set; }
        public int? EndLine { get; set; }
        public string? Comments { get; set; }
        public string? Message { get; set; }
        public string? Reasoning { get; set; }
        public string? Severity { get; set; }
    }
}
