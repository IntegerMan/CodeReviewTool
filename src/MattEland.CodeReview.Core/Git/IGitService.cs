using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Git;

/// <summary>
/// Abstraction for git repository operations.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Gets the diff between the current HEAD and the specified base branch.
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository.</param>
    /// <param name="baseBranch">The base branch to compare against (e.g., "main").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The diff between the branches.</returns>
    Task<GitDiff> GetDiffFromBaseAsync(
        string repositoryPath, 
        string baseBranch = "main",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the diff for specific files.
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository.</param>
    /// <param name="filePaths">The files to get diffs for.</param>
    /// <param name="baseBranch">The base branch to compare against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The diff for the specified files.</returns>
    Task<GitDiff> GetDiffForFilesAsync(
        string repositoryPath,
        IEnumerable<string> filePaths,
        string baseBranch = "main",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current branch name.
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository.</param>
    /// <returns>The current branch name.</returns>
    string GetCurrentBranch(string repositoryPath);

    /// <summary>
    /// Checks if a path is a valid git repository.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is a valid git repository.</returns>
    bool IsValidRepository(string path);

    /// <summary>
    /// Gets the repository root directory.
    /// </summary>
    /// <param name="path">Any path within the repository.</param>
    /// <returns>The repository root directory, or null if not in a repository.</returns>
    string? GetRepositoryRoot(string path);
}
