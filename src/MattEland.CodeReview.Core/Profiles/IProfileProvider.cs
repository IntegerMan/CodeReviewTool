using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Profiles;

/// <summary>
/// Provides access to review profiles.
/// </summary>
public interface IProfileProvider
{
    /// <summary>
    /// Gets a profile by its unique identifier.
    /// </summary>
    /// <param name="profileId">The profile ID (e.g., "confusing-code").</param>
    /// <returns>The profile, or null if not found.</returns>
    ReviewProfile? GetProfile(string profileId);

    /// <summary>
    /// Gets all enabled profiles.
    /// </summary>
    /// <returns>All profiles that are currently enabled.</returns>
    IEnumerable<ReviewProfile> GetEnabledProfiles();

    /// <summary>
    /// Gets all available profiles.
    /// </summary>
    /// <returns>All registered profiles.</returns>
    IEnumerable<ReviewProfile> GetAllProfiles();
}
