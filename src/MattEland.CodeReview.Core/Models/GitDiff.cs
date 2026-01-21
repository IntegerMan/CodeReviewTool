using System.ComponentModel;

namespace MattEland.CodeReview.Core.Models;

/// <summary>
/// Represents the complete diff between two git references.
/// </summary>
public sealed record GitDiff
{
    /// <summary>
    /// The base reference (e.g., "main", commit SHA).
    /// </summary>
    public required string BaseRef { get; init; }

    /// <summary>
    /// The head reference being compared (e.g., "feature-branch", "HEAD").
    /// </summary>
    public required string HeadRef { get; init; }

    /// <summary>
    /// The list of files that changed between the two references.
    /// </summary>
    public required IReadOnlyList<FileChange> Files { get; init; }

    /// <summary>
    /// Total number of lines added across all files.
    /// </summary>
    public int TotalLinesAdded => Files.Sum(f => f.LinesAdded);

    /// <summary>
    /// Total number of lines deleted across all files.
    /// </summary>
    public int TotalLinesDeleted => Files.Sum(f => f.LinesDeleted);

    /// <summary>
    /// Gets files filtered by language.
    /// </summary>
    public IEnumerable<FileChange> GetFilesByLanguage(string language)
        => Files.Where(f => string.Equals(f.Language, language, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the raw diff content for a specific file.
    /// </summary>
    public string? GetDiffForFile(string path)
        => Files.FirstOrDefault(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase))?.DiffContent;
}
