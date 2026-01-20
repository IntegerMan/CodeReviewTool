using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MattEland.CodeReview.Desktop.Services;

/// <summary>
/// Service for managing user settings persistence to a local configuration file.
/// </summary>
public class UserSettingsService : IUserSettingsService
{
    private const string AppFolderName = "MattEland.CodeReview";
    private const string SettingsFileName = "settings.json";
    private const int MaxRecentRepositories = 8;

    private readonly ILogger<UserSettingsService> _logger;
    private readonly string _settingsFilePath;
    private UserSettings _settings = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UserSettingsService(ILogger<UserSettingsService> logger)
    {
        _logger = logger;
        _settingsFilePath = GetSettingsFilePath();
    }

    /// <inheritdoc />
    public UserSettings Settings => _settings;

    /// <inheritdoc />
    public async Task<UserSettings> LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
                
                if (settings != null)
                {
                    _settings = settings;
                    _logger.LogInformation("Loaded user settings from {Path}", _settingsFilePath);
                }
            }
            else
            {
                _logger.LogInformation("No existing settings file found at {Path}, using defaults", _settingsFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load user settings from {Path}, using defaults", _settingsFilePath);
            _settings = new UserSettings();
        }

        return _settings;
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            
            _logger.LogInformation("Saved user settings to {Path}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user settings to {Path}", _settingsFilePath);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RecentRepositoryEntry> GetRecentRepositories()
    {
        return _settings.RecentRepositories.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task AddRecentRepositoryAsync(string path, string? lastBranch)
    {
        // Remove existing entry with the same path (case-insensitive)
        var existing = _settings.RecentRepositories
            .FirstOrDefault(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null)
        {
            _settings.RecentRepositories.Remove(existing);
        }

        // Add new entry at the beginning
        _settings.RecentRepositories.Insert(0, new RecentRepositoryEntry
        {
            Path = path,
            LastBranch = lastBranch,
            LastOpened = DateTime.Now
        });

        // Keep only the most recent entries
        while (_settings.RecentRepositories.Count > MaxRecentRepositories)
        {
            _settings.RecentRepositories.RemoveAt(_settings.RecentRepositories.Count - 1);
        }

        await SaveAsync();
    }

    /// <inheritdoc />
    public async Task RemoveRecentRepositoryAsync(string path)
    {
        var existing = _settings.RecentRepositories
            .FirstOrDefault(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null)
        {
            _settings.RecentRepositories.Remove(existing);
            await SaveAsync();
        }
    }

    private static string GetSettingsFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, AppFolderName, SettingsFileName);
    }
}
