using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Prompts;

/// <summary>
/// Loads and parses .prompt files.
/// </summary>
public interface IPromptLoader
{
    /// <summary>
    /// Loads a rule from a .prompt file.
    /// </summary>
    /// <param name="filePath">Path to the .prompt file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded rule with prompt content.</returns>
    Task<Rule> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a rule from embedded resource content.
    /// </summary>
    /// <param name="resourceName">Name of the embedded resource.</param>
    /// <param name="content">The content of the prompt file.</param>
    /// <returns>The loaded rule with prompt content.</returns>
    Rule LoadFromContent(string resourceName, string content);

    /// <summary>
    /// Renders a prompt template with the given context.
    /// </summary>
    /// <param name="rule">The rule containing the prompt template.</param>
    /// <param name="context">The context values to substitute.</param>
    /// <returns>The rendered prompt ready for the LLM.</returns>
    string RenderPrompt(Rule rule, PromptContext context);
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
