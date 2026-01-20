namespace MattEland.CodeReview.Core.Models;

/// <summary>
/// Represents a file that was changed in a git diff.
/// </summary>
public sealed record FileChange
{
    /// <summary>
    /// The path to the file relative to the repository root.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The type of change made to the file.
    /// </summary>
    public required FileChangeType ChangeType { get; init; }

    /// <summary>
    /// The previous path of the file (for renamed files).
    /// </summary>
    public string? OldPath { get; init; }

    /// <summary>
    /// The raw diff content for this file.
    /// </summary>
    public required string DiffContent { get; init; }

    /// <summary>
    /// Number of lines added.
    /// </summary>
    public int LinesAdded { get; init; }

    /// <summary>
    /// Number of lines deleted.
    /// </summary>
    public int LinesDeleted { get; init; }

    /// <summary>
    /// The detected programming language of the file.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// The full content of the file after the change (if available).
    /// </summary>
    public string? NewContent { get; init; }

    /// <summary>
    /// The full content of the file before the change (if available).
    /// </summary>
    public string? OldContent { get; init; }
}

/// <summary>
/// The type of change made to a file.
/// </summary>
public enum FileChangeType
{
    /// <summary>
    /// A new file was added.
    /// </summary>
    Added,

    /// <summary>
    /// An existing file was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// A file was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// A file was renamed.
    /// </summary>
    Renamed,

    /// <summary>
    /// A file was copied.
    /// </summary>
    Copied
}
