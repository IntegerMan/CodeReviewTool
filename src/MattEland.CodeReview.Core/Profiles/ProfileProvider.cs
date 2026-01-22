using MattEland.CodeReview.Core.Models;
using Microsoft.Extensions.Logging;

namespace MattEland.CodeReview.Core.Profiles;

/// <summary>
/// Implementation of <see cref="IProfileProvider"/> that aggregates profiles from multiple sources.
/// </summary>
public sealed class ProfileProvider : IProfileProvider
{
    private readonly ILogger<ProfileProvider> _logger;
    private readonly Dictionary<string, ReviewProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    public ProfileProvider(IEnumerable<IProfileSource> sources, ILogger<ProfileProvider> logger)
    {
        Sources = sources.OrderByDescending(s => s.Priority).ToList();
        _logger = logger;
    }

    /// <summary>
    /// Gets the profile sources.
    /// </summary>
    public IReadOnlyList<IProfileSource> Sources { get; }

    /// <summary>
    /// Initializes the provider by loading profiles from all sources.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        foreach (var source in Sources)
        {
            try
            {
                _logger.LogInformation("Loading profiles from source: {SourceName}", source.Name);
                var profiles = await source.DiscoverProfilesAsync(cancellationToken);
                
                foreach (var profile in profiles)
                {
                    if (_profiles.TryAdd(profile.Id, profile))
                    {
                        _logger.LogDebug("Loaded profile: {ProfileId}", profile.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate profile ID: {ProfileId} from source {SourceName}", 
                            profile.Id, source.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profiles from source: {SourceName}", source.Name);
            }
        }

        _initialized = true;
        _logger.LogInformation("Loaded {Count} profiles from {SourceCount} sources", 
            _profiles.Count, Sources.Count);
    }

    /// <inheritdoc />
    public ReviewProfile? GetProfile(string profileId)
    {
        EnsureInitialized();
        return _profiles.GetValueOrDefault(profileId);
    }

    /// <inheritdoc />
    public IEnumerable<ReviewProfile> GetEnabledProfiles()
    {
        EnsureInitialized();
        return _profiles.Values.Where(p => p.Enabled);
    }

    /// <inheritdoc />
    public IEnumerable<ReviewProfile> GetAllProfiles()
    {
        EnsureInitialized();
        return _profiles.Values;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "ProfileProvider has not been initialized. Call InitializeAsync first.");
        }
    }
}
