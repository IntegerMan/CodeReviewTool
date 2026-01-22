using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Profiles;

/// <summary>
/// A source that can discover and load review profiles.
/// </summary>
public interface IProfileSource
{
    /// <summary>
    /// Gets the name of this profile source.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority of this source. Higher priority sources are loaded first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Discovers all profiles from this source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All profiles discovered from this source.</returns>
    Task<IEnumerable<ReviewProfile>> DiscoverProfilesAsync(CancellationToken cancellationToken = default);
}
