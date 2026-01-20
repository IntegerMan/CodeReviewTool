using MattEland.CodeReview.Desktop.Services;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Home page with repository selection and recent repos.
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly IGitService _gitService;
    private readonly IRuleProvider _ruleProvider;
    private readonly IUserSettingsService _settingsService;

    public HomeViewModel(
        MainViewModel mainViewModel, 
        IGitService gitService, 
        IRuleProvider ruleProvider,
        IUserSettingsService settingsService)
    {
        _mainViewModel = mainViewModel;
        _gitService = gitService;
        _ruleProvider = ruleProvider;
        _settingsService = settingsService;
        LoadRecentRepositories();
    }

    /// <summary>
    /// Recently opened repositories.
    /// </summary>
    public ObservableCollection<RecentRepository> RecentRepositories { get; } = [];

    /// <summary>
    /// Whether a repository is currently open.
    /// </summary>
    public bool HasRepository => _mainViewModel.HasRepository;

    /// <summary>
    /// Current repository path.
    /// </summary>
    public string? RepositoryPath => _mainViewModel.RepositoryPath;

    /// <summary>
    /// Current repository display name.
    /// </summary>
    public string RepositoryDisplayName => _mainViewModel.RepositoryDisplayName;

    /// <summary>
    /// Current branch name.
    /// </summary>
    public string? CurrentBranch => _mainViewModel.CurrentBranch;

    /// <summary>
    /// Total number of available rules.
    /// </summary>
    public int TotalRules => _ruleProvider.GetAllRules().Count();

    /// <summary>
    /// Number of enabled rules.
    /// </summary>
    public int EnabledRules => _ruleProvider.GetEnabledRules().Count();

    /// <summary>
    /// Number of supported languages.
    /// </summary>
    public int SupportedLanguages => _ruleProvider.GetLanguages().Count();

    /// <summary>
    /// Opens a folder picker to select a repository.
    /// </summary>
    [RelayCommand]
    private async Task BrowseForRepositoryAsync()
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        folderPicker.FileTypeFilter.Add("*");

        // Get the window handle for the picker
        var window = App.MainWindow;
        if (window == null) return;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            await OpenRepositoryAsync(folder.Path);
        }
    }

    /// <summary>
    /// Opens a repository from a path.
    /// </summary>
    [RelayCommand]
    private async Task OpenRepositoryAsync(string path)
    {
        if (_mainViewModel.TrySetRepository(path))
        {
            await AddToRecentRepositoriesAsync(path);
            OnPropertyChanged(nameof(HasRepository));
            OnPropertyChanged(nameof(RepositoryPath));
            OnPropertyChanged(nameof(RepositoryDisplayName));
            OnPropertyChanged(nameof(CurrentBranch));
        }
    }

    /// <summary>
    /// Removes a repository from the recent list.
    /// </summary>
    [RelayCommand]
    private async Task RemoveFromRecentAsync(RecentRepository repo)
    {
        RecentRepositories.Remove(repo);
        await _settingsService.RemoveRecentRepositoryAsync(repo.Path);
    }

    /// <summary>
    /// Navigates to the Analysis page.
    /// </summary>
    [RelayCommand]
    private void NavigateToAnalysis()
    {
        _mainViewModel.RequestNavigateToAnalysis();
    }

    private async Task AddToRecentRepositoriesAsync(string path)
    {
        var existing = RecentRepositories.FirstOrDefault(r => 
            string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null)
        {
            RecentRepositories.Remove(existing);
        }

        var displayName = Path.GetFileName(path) ?? path;
        var branch = _gitService.IsValidRepository(path) 
            ? _gitService.GetCurrentBranch(path) 
            : null;

        RecentRepositories.Insert(0, new RecentRepository
        {
            Path = path,
            DisplayName = displayName,
            LastBranch = branch,
            LastOpened = DateTime.Now
        });

        // Keep only the last 8 (settings service enforces this limit too)
        while (RecentRepositories.Count > 8)
        {
            RecentRepositories.RemoveAt(RecentRepositories.Count - 1);
        }

        // Persist to settings
        await _settingsService.AddRecentRepositoryAsync(path, branch);
    }

    private void LoadRecentRepositories()
    {
        RecentRepositories.Clear();
        
        foreach (var entry in _settingsService.GetRecentRepositories())
        {
            var displayName = Path.GetFileName(entry.Path) ?? entry.Path;
            
            // Update branch info if the repository still exists
            string? currentBranch = entry.LastBranch;
            if (_gitService.IsValidRepository(entry.Path))
            {
                currentBranch = _gitService.GetCurrentBranch(entry.Path);
            }

            RecentRepositories.Add(new RecentRepository
            {
                Path = entry.Path,
                DisplayName = displayName,
                LastBranch = currentBranch,
                LastOpened = entry.LastOpened
            });
        }
    }
}

/// <summary>
/// Represents a recently opened repository.
/// </summary>
public class RecentRepository
{
    public required string Path { get; init; }
    public required string DisplayName { get; init; }
    public string? LastBranch { get; init; }
    public DateTime LastOpened { get; init; }
}
