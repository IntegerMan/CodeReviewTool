using System.ComponentModel;

namespace MattEland.CodeReview.Core.Models;

/// <summary>
/// Represents a single issue detected during code review.
/// </summary>
public sealed record Issue
{
    /// <summary>
    /// Unique identifier for this issue instance.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The rule that detected this issue.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// The file path where the issue was found.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The line number where the issue starts (1-based).
    /// </summary>
    public int? StartLine { get; init; }

    /// <summary>
    /// The line number where the issue ends (1-based).
    /// </summary>
    public int? EndLine { get; init; }

    /// <summary>
    /// The severity of this issue.
    /// </summary>
    public required Severity Severity { get; init; }

    /// <summary>
    /// Human-readable message describing the issue.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Suggested fix or improvement.
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// The code snippet related to this issue.
    /// </summary>
    public string? CodeSnippet { get; init; }

    /// <summary>
    /// Additional context or explanation from the LLM.
    /// </summary>
    public string? Explanation { get; init; }

    /// <summary>
    /// Confidence score from the LLM (0.0 to 1.0).
    /// </summary>
    public double? Confidence { get; init; }
}
