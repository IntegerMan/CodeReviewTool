using MattEland.CodeReview.Core.Configuration;

namespace MattEland.CodeReview.Desktop.Services;

/// <summary>
/// User settings that are persisted to a local configuration file.
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Code review configuration options.
    /// </summary>
    public CodeReviewOptions CodeReview { get; set; } = new();

    /// <summary>
    /// List of recently opened repositories.
    /// </summary>
    public List<RecentRepositoryEntry> RecentRepositories { get; set; } = [];
}

/// <summary>
/// Represents a recently opened repository entry for persistence.
/// </summary>
public class RecentRepositoryEntry
{
    /// <summary>
    /// The file system path to the repository.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The last known branch that was checked out.
    /// </summary>
    public string? LastBranch { get; set; }

    /// <summary>
    /// When the repository was last opened.
    /// </summary>
    public DateTime LastOpened { get; set; }
}
