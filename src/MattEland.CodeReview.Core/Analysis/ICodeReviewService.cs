using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Analysis;

/// <summary>
/// Main service for performing code reviews.
/// </summary>
public interface ICodeReviewService
{
    /// <summary>
    /// Analyzes the diff between the current branch and the specified base branch.
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository.</param>
    /// <param name="baseBranch">The base branch to compare against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The review result containing all detected issues.</returns>
    Task<ReviewResult> AnalyzeDiffAsync(
        string repositoryPath,
        string baseBranch = "main",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes specific files in the repository.
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository.</param>
    /// <param name="filePaths">The files to analyze.</param>
    /// <param name="baseBranch">The base branch to compare against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The review result containing all detected issues.</returns>
    Task<ReviewResult> AnalyzeFilesAsync(
        string repositoryPath,
        IEnumerable<string> filePaths,
        string baseBranch = "main",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a pre-computed git diff.
    /// </summary>
    /// <param name="diff">The diff to analyze.</param>
    /// <param name="progressReporter">Optional progress reporter for detailed updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The review result containing all detected issues.</returns>
    Task<ReviewResult> AnalyzeDiffAsync(
        GitDiff diff,
        IAnalysisProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a pre-computed git diff with a specific set of rules.
    /// </summary>
    /// <param name="diff">The diff to analyze.</param>
    /// <param name="selectedRuleIds">The IDs of rules to apply. If null, all enabled rules are used.</param>
    /// <param name="progressReporter">Optional progress reporter for detailed updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The review result containing all detected issues.</returns>
    Task<ReviewResult> AnalyzeDiffAsync(
        GitDiff diff,
        IEnumerable<string>? selectedRuleIds,
        IAnalysisProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default);
}
