namespace MattEland.CodeReview.Core.Models;

/// <summary>
/// Represents a review profile that defines how to analyze code for specific patterns.
/// </summary>
public sealed record ReviewProfile
{
    /// <summary>
    /// Unique identifier for the profile (e.g., "confusing-code").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable display name for the profile.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Brief description of what the profile detects.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Whether this profile is enabled for analysis.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Path to the .profile file containing the analysis instructions.
    /// </summary>
    public string? ProfilePath { get; init; }

    /// <summary>
    /// The prompt content (loaded from file or embedded resource).
    /// </summary>
    public string? PromptContent { get; init; }

    /// <summary>
    /// Indicates whether this profile was loaded from an embedded resource.
    /// </summary>
    public bool IsEmbedded { get; init; }
}
