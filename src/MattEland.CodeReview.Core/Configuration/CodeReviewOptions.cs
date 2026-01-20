namespace MattEland.CodeReview.Core.Configuration;

/// <summary>
/// Root configuration options for the code review tool.
/// </summary>
public sealed class CodeReviewOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "CodeReview";

    /// <summary>
    /// The AI provider to use (AzureOpenAI, OpenAI, or Ollama).
    /// </summary>
    public string Provider { get; set; } = "Ollama";

    /// <summary>
    /// Azure OpenAI configuration.
    /// </summary>
    public AzureOpenAIOptions AzureOpenAI { get; set; } = new();

    /// <summary>
    /// OpenAI configuration.
    /// </summary>
    public OpenAIOptions OpenAI { get; set; } = new();

    /// <summary>
    /// Ollama configuration.
    /// </summary>
    public OllamaOptions Ollama { get; set; } = new();

    /// <summary>
    /// Git-related configuration.
    /// </summary>
    public GitOptions Git { get; set; } = new();

    /// <summary>
    /// Paths to search for custom prompt files.
    /// </summary>
    public List<string> CustomPromptPaths { get; set; } = [];
}

/// <summary>
/// Azure OpenAI provider configuration.
/// </summary>
public sealed class AzureOpenAIOptions
{
    /// <summary>
    /// The Azure OpenAI endpoint URL.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// The deployment name to use.
    /// </summary>
    public string DeploymentName { get; set; } = "gpt-4o";

    /// <summary>
    /// The API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// OpenAI provider configuration.
/// </summary>
public sealed class OpenAIOptions
{
    /// <summary>
    /// The OpenAI API key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The model to use.
    /// </summary>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>
    /// Optional organization ID.
    /// </summary>
    public string? OrganizationId { get; set; }
}

/// <summary>
/// Ollama provider configuration.
/// </summary>
public sealed class OllamaOptions
{
    /// <summary>
    /// The Ollama server endpoint.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// The model to use.
    /// </summary>
    public string Model { get; set; } = "llama3";
}

/// <summary>
/// Git-related configuration.
/// </summary>
public sealed class GitOptions
{
    /// <summary>
    /// The default base branch for comparisons.
    /// </summary>
    public string DefaultBaseBranch { get; set; } = "main";

    /// <summary>
    /// Maximum number of files to analyze in a single review.
    /// </summary>
    public int MaxFilesPerReview { get; set; } = 50;

    /// <summary>
    /// Maximum diff size per file (in characters) before truncation.
    /// </summary>
    public int MaxDiffSizePerFile { get; set; } = 10000;
}
