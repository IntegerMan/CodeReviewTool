using MattEland.CodeReview.Core.Models;
using Microsoft.Extensions.Logging;

namespace MattEland.CodeReview.Core.Rules;

/// <summary>
/// Implementation of <see cref="IRuleProvider"/> that aggregates rules from multiple sources.
/// </summary>
public sealed class RuleProvider : IRuleProvider
{
    private readonly ILogger<RuleProvider> _logger;
    private readonly Dictionary<string, Rule> _rules = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    public RuleProvider(IEnumerable<IRuleSource> sources, ILogger<RuleProvider> logger)
    {
        Sources = sources.OrderByDescending(s => s.Priority).ToList();
        _logger = logger;
    }

    /// <summary>
    /// Gets the rule sources.
    /// </summary>
    public IReadOnlyList<IRuleSource> Sources { get; }

    /// <summary>
    /// Initializes the provider by loading rules from all sources.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        foreach (var source in Sources)
        {
            try
            {
                _logger.LogInformation("Loading rules from source: {SourceName}", source.Name);
                var rules = await source.DiscoverRulesAsync(cancellationToken);
                
                foreach (var rule in rules)
                {
                    if (_rules.TryAdd(rule.Id, rule))
                    {
                        _logger.LogDebug("Loaded rule: {RuleId}", rule.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate rule ID: {RuleId} from source {SourceName}", 
                            rule.Id, source.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load rules from source: {SourceName}", source.Name);
            }
        }

        _initialized = true;
        _logger.LogInformation("Loaded {Count} rules from {SourceCount} sources", 
            _rules.Count, Sources.Count);
    }

    /// <inheritdoc />
    public Rule? GetRule(string ruleId)
    {
        EnsureInitialized();
        return _rules.GetValueOrDefault(ruleId);
    }

    /// <inheritdoc />
    public IEnumerable<Rule> GetRulesByLanguage(string language)
    {
        EnsureInitialized();
        return _rules.Values.Where(r => 
            string.Equals(r.Language, language, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IEnumerable<Rule> GetRulesByCategory(string language, string category)
    {
        EnsureInitialized();
        return _rules.Values.Where(r =>
            string.Equals(r.Language, language, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IEnumerable<Rule> GetEnabledRules()
    {
        EnsureInitialized();
        return _rules.Values.Where(r => r.Enabled);
    }

    /// <inheritdoc />
    public IEnumerable<Rule> GetAllRules()
    {
        EnsureInitialized();
        return _rules.Values;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetLanguages()
    {
        EnsureInitialized();
        return _rules.Values.Select(r => r.Language).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetCategories(string language)
    {
        EnsureInitialized();
        return GetRulesByLanguage(language)
            .Select(r => r.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "RuleProvider has not been initialized. Call InitializeAsync first.");
        }
    }
}
