namespace MattEland.CodeReview.Desktop.Services;

/// <summary>
/// Service for managing user settings persistence.
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Gets the current user settings.
    /// </summary>
    UserSettings Settings { get; }

    /// <summary>
    /// Loads user settings from the configuration file.
    /// </summary>
    /// <returns>The loaded user settings.</returns>
    Task<UserSettings> LoadAsync();

    /// <summary>
    /// Saves the current user settings to the configuration file.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Gets the list of recent repositories.
    /// </summary>
    /// <returns>List of recent repository entries.</returns>
    IReadOnlyList<RecentRepositoryEntry> GetRecentRepositories();

    /// <summary>
    /// Adds or updates a repository in the recent list.
    /// </summary>
    /// <param name="path">The repository path.</param>
    /// <param name="lastBranch">The last known branch.</param>
    Task AddRecentRepositoryAsync(string path, string? lastBranch);

    /// <summary>
    /// Removes a repository from the recent list.
    /// </summary>
    /// <param name="path">The repository path to remove.</param>
    Task RemoveRecentRepositoryAsync(string path);
}
