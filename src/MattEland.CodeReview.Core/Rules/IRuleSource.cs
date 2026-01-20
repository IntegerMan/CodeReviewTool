using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Rules;

/// <summary>
/// A source that can discover and load rules.
/// </summary>
public interface IRuleSource
{
    /// <summary>
    /// Gets the name of this rule source.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority of this source. Higher priority sources are loaded first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Discovers all rules from this source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All rules discovered from this source.</returns>
    Task<IEnumerable<Rule>> DiscoverRulesAsync(CancellationToken cancellationToken = default);
}
