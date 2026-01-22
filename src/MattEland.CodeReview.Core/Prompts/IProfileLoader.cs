using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Prompts;

/// <summary>
/// Loads and parses .profile files.
/// </summary>
public interface IProfileLoader
{
    /// <summary>
    /// Loads a review profile from a .profile file.
    /// </summary>
    /// <param name="filePath">Path to the .profile file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded profile with prompt content.</returns>
    Task<ReviewProfile> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a review profile from embedded resource content.
    /// </summary>
    /// <param name="resourceName">Name of the embedded resource.</param>
    /// <param name="content">The content of the profile file.</param>
    /// <returns>The loaded profile with prompt content.</returns>
    ReviewProfile LoadFromContent(string resourceName, string content);

    /// <summary>
    /// Renders a prompt template with the given context.
    /// </summary>
    /// <param name="profile">The profile containing the prompt template.</param>
    /// <param name="context">The context values to substitute.</param>
    /// <returns>The rendered prompt ready for the LLM.</returns>
    string RenderPrompt(ReviewProfile profile, PromptContext context);
}

/// <summary>
/// Context for rendering a prompt template.
/// </summary>
public sealed record PromptContext
{
    /// <summary>
    /// The diff content to analyze.
    /// </summary>
    public required string Diff { get; init; }

    /// <summary>
    /// The file path being analyzed.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// The programming language of the file.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Additional context-specific values.
    /// </summary>
    public IDictionary<string, string> AdditionalValues { get; init; } = new Dictionary<string, string>();
}

