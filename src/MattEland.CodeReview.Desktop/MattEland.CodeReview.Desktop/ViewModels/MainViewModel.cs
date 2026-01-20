namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// Main application ViewModel managing app-level state and navigation.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IGitService _gitService;

    public MainViewModel(IGitService gitService)
    {
        _gitService = gitService;
    }

    /// <summary>
    /// The path to the currently open repository.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRepository))]
    [NotifyPropertyChangedFor(nameof(RepositoryDisplayName))]
    [NotifyPropertyChangedFor(nameof(CurrentBranch))]
    private string? _repositoryPath;

    /// <summary>
    /// Whether a repository is currently open.
    /// </summary>
    public bool HasRepository => !string.IsNullOrEmpty(RepositoryPath) && _gitService.IsValidRepository(RepositoryPath);

    /// <summary>
    /// Display name for the repository (folder name).
    /// </summary>
    public string RepositoryDisplayName => HasRepository 
        ? Path.GetFileName(RepositoryPath!) ?? "Repository" 
        : "No Repository";

    /// <summary>
    /// Current branch name if a repository is open.
    /// </summary>
    public string? CurrentBranch => HasRepository 
        ? _gitService.GetCurrentBranch(RepositoryPath!) 
        : null;

    /// <summary>
    /// Whether the app is currently busy with an operation.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Status message to display.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// Sets the repository path and validates it.
    /// </summary>
    public bool TrySetRepository(string path)
    {
        var root = _gitService.GetRepositoryRoot(path);
        if (root != null)
        {
            RepositoryPath = root;
            StatusMessage = $"Opened repository: {RepositoryDisplayName}";
            return true;
        }
        
        StatusMessage = "Invalid repository path";
        return false;
    }

    /// <summary>
    /// Event raised when navigation to a specific page is requested.
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    /// <summary>
    /// Requests navigation to the Analysis page.
    /// </summary>
    public void RequestNavigateToAnalysis()
    {
        NavigationRequested?.Invoke(this, "AnalysisPage");
    }
}
