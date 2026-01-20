using Azure;
using Azure.AI.Inference;
using MattEland.CodeReview.Core.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MattEland.CodeReview.Core.Analysis;

/// <summary>
/// Factory for creating <see cref="IChatClient"/> instances based on configuration.
/// </summary>
public sealed class ChatClientFactory
{
    private readonly CodeReviewOptions _options;
    private readonly ILogger<ChatClientFactory> _logger;

    public ChatClientFactory(IOptions<CodeReviewOptions> options, ILogger<ChatClientFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates an <see cref="IChatClient"/> based on the configured provider.
    /// </summary>
    public IChatClient CreateClient()
    {
        var provider = _options.Provider?.ToLowerInvariant() ?? "ollama";
        
        _logger.LogInformation("Creating chat client for provider: {Provider}", provider);

        return provider switch
        {
            "azureopenai" => CreateAzureOpenAIClient(),
            "openai" => CreateOpenAIClient(),
            "ollama" => CreateOllamaClient(),
            _ => throw new InvalidOperationException($"Unknown AI provider: {provider}")
        };
    }

    private IChatClient CreateAzureOpenAIClient()
    {
        var config = _options.AzureOpenAI;
        
        if (string.IsNullOrEmpty(config.Endpoint))
            throw new InvalidOperationException("Azure OpenAI endpoint is required");
        if (string.IsNullOrEmpty(config.ApiKey))
            throw new InvalidOperationException("Azure OpenAI API key is required");

        _logger.LogDebug("Connecting to Azure OpenAI at {Endpoint} with deployment {Deployment}",
            config.Endpoint, config.DeploymentName);

        var client = new ChatCompletionsClient(
            new Uri($"{config.Endpoint.TrimEnd('/')}/openai/deployments/{config.DeploymentName}"),
            new AzureKeyCredential(config.ApiKey));

        return client.AsIChatClient(config.DeploymentName);
    }

    private IChatClient CreateOpenAIClient()
    {
        var config = _options.OpenAI;
        
        if (string.IsNullOrEmpty(config.ApiKey))
            throw new InvalidOperationException("OpenAI API key is required");

        _logger.LogDebug("Connecting to OpenAI with model {Model}", config.Model);

        var client = new OpenAI.Chat.ChatClient(config.Model, config.ApiKey);
        return client.AsIChatClient();
    }

    private IChatClient CreateOllamaClient()
    {
        var config = _options.Ollama;
        
        _logger.LogDebug("Connecting to Ollama at {Endpoint} with model {Model}",
            config.Endpoint, config.Model);

        return new OllamaChatClient(new Uri(config.Endpoint), config.Model);
    }
}
