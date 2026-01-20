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

    public HomeViewModel(
        MainViewModel mainViewModel, 
        IGitService gitService, 
        IRuleProvider ruleProvider)
    {
        _mainViewModel = mainViewModel;
        _gitService = gitService;
        _ruleProvider = ruleProvider;
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
            OpenRepository(folder.Path);
        }
    }

    /// <summary>
    /// Opens a repository from a path.
    /// </summary>
    [RelayCommand]
    private void OpenRepository(string path)
    {
        if (_mainViewModel.TrySetRepository(path))
        {
            AddToRecentRepositories(path);
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
    private void RemoveFromRecent(RecentRepository repo)
    {
        RecentRepositories.Remove(repo);
        SaveRecentRepositories();
    }

    /// <summary>
    /// Navigates to the Analysis page.
    /// </summary>
    [RelayCommand]
    private void NavigateToAnalysis()
    {
        _mainViewModel.RequestNavigateToAnalysis();
    }

    private void AddToRecentRepositories(string path)
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

        // Keep only the last 10
        while (RecentRepositories.Count > 10)
        {
            RecentRepositories.RemoveAt(RecentRepositories.Count - 1);
        }

        SaveRecentRepositories();
    }

    private void LoadRecentRepositories()
    {
        // TODO: Load from local storage/settings
        // For now, start with empty list
    }

    private void SaveRecentRepositories()
    {
        // TODO: Save to local storage/settings
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
