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
    /// The review profile that detected this issue.
    /// </summary>
    public required string ProfileId { get; init; }

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
    /// Human-readable message describing the issue (comments from the reviewer).
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// AI reasoning explaining why this is an issue.
    /// </summary>
    public string? Reasoning { get; init; }

    /// <summary>
    /// The code snippet related to this issue.
    /// </summary>
    public string? CodeSnippet { get; init; }

    /// <summary>
    /// Error message explaining why a code snippet could not be extracted.
    /// </summary>
    public string? CodeSnippetError { get; init; }
}
