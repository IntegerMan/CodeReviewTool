using MattEland.CodeReview.Core.Models;
using MattEland.CodeReview.Core.Prompts;
using Microsoft.Extensions.Logging;

namespace MattEland.CodeReview.Core.Rules;

/// <summary>
/// Rule source that loads .prompt files from the file system.
/// </summary>
public sealed class FileSystemRuleSource : IRuleSource
{
    private readonly IPromptLoader _promptLoader;
    private readonly ILogger<FileSystemRuleSource> _logger;
    private readonly List<string> _searchPaths;

    public FileSystemRuleSource(
        IPromptLoader promptLoader,
        ILogger<FileSystemRuleSource> logger,
        IEnumerable<string>? additionalPaths = null)
    {
        _promptLoader = promptLoader;
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
    public int Priority => 100; // Higher priority - user rules can override defaults

    /// <inheritdoc />
    public async Task<IEnumerable<Rule>> DiscoverRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = new List<Rule>();

        foreach (var basePath in _searchPaths)
        {
            if (!Directory.Exists(basePath))
            {
                _logger.LogDebug("Prompt directory does not exist: {Path}", basePath);
                continue;
            }

            _logger.LogInformation("Scanning for .prompt files in: {Path}", basePath);
            
            var promptFiles = Directory.EnumerateFiles(basePath, "*.prompt", SearchOption.AllDirectories);
            
            foreach (var file in promptFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    var rule = await _promptLoader.LoadFromFileAsync(file, cancellationToken);
                    rules.Add(rule);
                    _logger.LogDebug("Loaded rule from file: {Path}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load prompt file: {Path}", file);
                }
            }
        }

        return rules;
    }

    private static IEnumerable<string> GetDefaultSearchPaths()
    {
        // Application prompts directory (relative to exe)
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        yield return Path.Combine(appDir, "prompts");
        
        // Working directory prompts
        yield return Path.Combine(Directory.GetCurrentDirectory(), "prompts");
        
        // User config directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrEmpty(appData))
        {
            yield return Path.Combine(appData, "MattEland.CodeReview", "prompts");
        }
        
        // Linux/macOS config directory
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(home))
        {
            yield return Path.Combine(home, ".config", "MattEland.CodeReview", "prompts");
        }
    }
}
