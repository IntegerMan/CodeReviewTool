namespace MattEland.CodeReview.Core.Models;

/// <summary>
/// Represents a code review rule that can detect specific issues in code.
/// </summary>
public sealed record Rule
{
    /// <summary>
    /// Unique identifier for the rule (e.g., "sql/performance/select-star").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The programming language this rule applies to (e.g., "csharp", "sql").
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    /// The category within the language (e.g., "performance", "security", "logic").
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Human-readable display name for the rule.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Brief description of what the rule detects.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Default severity level for issues detected by this rule.
    /// </summary>
    public Severity DefaultSeverity { get; init; } = Severity.Warning;

    /// <summary>
    /// Whether this rule is enabled for analysis.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Tags for filtering and grouping rules.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Path to the .prompt file containing the analysis instructions.
    /// </summary>
    public string? PromptPath { get; init; }

    /// <summary>
    /// The prompt content (loaded from file or embedded resource).
    /// </summary>
    public string? PromptContent { get; init; }

    /// <summary>
    /// Indicates whether this rule was loaded from an embedded resource.
    /// </summary>
    public bool IsEmbedded { get; init; }
}
