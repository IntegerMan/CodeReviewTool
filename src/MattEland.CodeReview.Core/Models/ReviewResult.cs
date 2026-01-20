namespace MattEland.CodeReview.Core.Models;

/// <summary>
/// Represents the complete result of a code review analysis.
/// </summary>
public sealed record ReviewResult
{
    /// <summary>
    /// Unique identifier for this review.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// When the review was started.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// When the review completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// The git diff that was analyzed.
    /// </summary>
    public required GitDiff Diff { get; init; }

    /// <summary>
    /// All issues found during the review.
    /// </summary>
    public required IReadOnlyList<Issue> Issues { get; init; }

    /// <summary>
    /// The rules that were applied during the review.
    /// </summary>
    public required IReadOnlyList<Rule> AppliedRules { get; init; }

    /// <summary>
    /// Whether the review completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Error message if the review failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Duration of the review.
    /// </summary>
    public TimeSpan? Duration => CompletedAt.HasValue 
        ? CompletedAt.Value - StartedAt 
        : null;

    /// <summary>
    /// Gets issues filtered by severity.
    /// </summary>
    public IEnumerable<Issue> GetIssuesBySeverity(Severity severity)
        => Issues.Where(i => i.Severity == severity);

    /// <summary>
    /// Gets issues filtered by rule ID.
    /// </summary>
    public IEnumerable<Issue> GetIssuesByRule(string ruleId)
        => Issues.Where(i => string.Equals(i.RuleId, ruleId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets issues for a specific file.
    /// </summary>
    public IEnumerable<Issue> GetIssuesForFile(string filePath)
        => Issues.Where(i => string.Equals(i.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets a summary of issue counts by severity.
    /// </summary>
    public IReadOnlyDictionary<Severity, int> GetSeveritySummary()
        => Issues
            .GroupBy(i => i.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Total number of critical issues.
    /// </summary>
    public int CriticalCount => Issues.Count(i => i.Severity == Severity.Critical);

    /// <summary>
    /// Total number of error issues.
    /// </summary>
    public int ErrorCount => Issues.Count(i => i.Severity == Severity.Error);

    /// <summary>
    /// Total number of warning issues.
    /// </summary>
    public int WarningCount => Issues.Count(i => i.Severity == Severity.Warning);

    /// <summary>
    /// Total number of info issues.
    /// </summary>
    public int InfoCount => Issues.Count(i => i.Severity == Severity.Info);
}
