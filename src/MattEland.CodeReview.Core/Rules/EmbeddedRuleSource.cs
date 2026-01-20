using System.Reflection;
using MattEland.CodeReview.Core.Models;
using MattEland.CodeReview.Core.Prompts;
using Microsoft.Extensions.Logging;

namespace MattEland.CodeReview.Core.Rules;

/// <summary>
/// Rule source that loads .prompt files from embedded resources.
/// </summary>
public sealed class EmbeddedRuleSource : IRuleSource
{
    private readonly IPromptLoader _promptLoader;
    private readonly ILogger<EmbeddedRuleSource> _logger;
    private readonly Assembly _assembly;

    public EmbeddedRuleSource(
        IPromptLoader promptLoader,
        ILogger<EmbeddedRuleSource> logger,
        Assembly? assembly = null)
    {
        _promptLoader = promptLoader;
        _logger = logger;
        _assembly = assembly ?? typeof(EmbeddedRuleSource).Assembly;
    }

    /// <inheritdoc />
    public string Name => "Embedded";

    /// <inheritdoc />
    public int Priority => 0; // Lower priority - defaults can be overridden

    /// <inheritdoc />
    public Task<IEnumerable<Rule>> DiscoverRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = new List<Rule>();
        var resourceNames = _assembly.GetManifestResourceNames()
            .Where(n => n.EndsWith(".prompt", StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in resourceNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                using var stream = _assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    _logger.LogWarning("Could not load embedded resource: {ResourceName}", resourceName);
                    continue;
                }

                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                
                var rule = _promptLoader.LoadFromContent(resourceName, content);
                rules.Add(rule with { IsEmbedded = true });
                
                _logger.LogDebug("Loaded embedded rule: {ResourceName}", resourceName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load embedded resource: {ResourceName}", resourceName);
            }
        }

        return Task.FromResult<IEnumerable<Rule>>(rules);
    }
}
