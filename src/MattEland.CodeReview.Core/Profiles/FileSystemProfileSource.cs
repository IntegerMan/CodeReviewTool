using MattEland.CodeReview.Core.Models;
using MattEland.CodeReview.Core.Prompts;
using Microsoft.Extensions.Logging;

namespace MattEland.CodeReview.Core.Profiles;

/// <summary>
/// Profile source that loads .profile files from the file system.
/// </summary>
public sealed class FileSystemProfileSource : IProfileSource
{
    private readonly IProfileLoader _profileLoader;
    private readonly ILogger<FileSystemProfileSource> _logger;
    private readonly List<string> _searchPaths;

    public FileSystemProfileSource(
        IProfileLoader profileLoader,
        ILogger<FileSystemProfileSource> logger,
        IEnumerable<string>? additionalPaths = null)
    {
        _profileLoader = profileLoader;
        _logger = logger;
        _searchPaths = GetDefaultSearchPaths().ToList();
        
        if (additionalPaths != null)
        {
            _searchPaths.AddRange(additionalPaths);
        }
    }

    /// <inheritdoc />
    public string Name => "FileSystem";

    /// <inheritdoc />
    public int Priority => 100; // Higher priority - user profiles can override defaults

    /// <inheritdoc />
    public async Task<IEnumerable<ReviewProfile>> DiscoverProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = new List<ReviewProfile>();

        foreach (var basePath in _searchPaths)
        {
            if (!Directory.Exists(basePath))
            {
                _logger.LogDebug("Profile directory does not exist: {Path}", basePath);
                continue;
            }

            _logger.LogInformation("Scanning for .profile files in: {Path}", basePath);
            
            var profileFiles = Directory.EnumerateFiles(basePath, "*.profile", SearchOption.AllDirectories);
            
            foreach (var file in profileFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    var profile = await _profileLoader.LoadFromFileAsync(file, cancellationToken);
                    profiles.Add(profile);
                    _logger.LogDebug("Loaded profile from file: {Path}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load profile file: {Path}", file);
                }
            }
        }

        return profiles;
    }

    private static IEnumerable<string> GetDefaultSearchPaths()
    {
        // Application prompts directory (relative to exe)
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        yield return Path.Combine(appDir, "prompts", "profiles");
        
        // Working directory prompts
        yield return Path.Combine(Directory.GetCurrentDirectory(), "prompts", "profiles");
        
        // User config directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrEmpty(appData))
        {
            yield return Path.Combine(appData, "MattEland.CodeReview", "profiles");
        }
        
        // Linux/macOS config directory
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(home))
        {
            yield return Path.Combine(home, ".config", "MattEland.CodeReview", "profiles");
        }
    }
}
