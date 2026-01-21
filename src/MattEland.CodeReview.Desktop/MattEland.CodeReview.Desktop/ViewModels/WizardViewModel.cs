using System.ComponentModel;

namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// Wizard step enumeration.
/// </summary>
public enum WizardStep
{
    Repository = 0,
    Files = 1,
    Rules = 2,
    Analysis = 3
}

/// <summary>
/// ViewModel managing the wizard flow for code review analysis.
/// </summary>
[Bindable(true)]
public partial class WizardViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly IGitService _gitService;
    private readonly IRuleProvider _ruleProvider;
    private readonly ICodeReviewService _codeReviewService;

    public WizardViewModel(
        MainViewModel mainViewModel,
        IGitService gitService,
        IRuleProvider ruleProvider,
        ICodeReviewService codeReviewService)
    {
        _mainViewModel = mainViewModel;
        _gitService = gitService;
        _ruleProvider = ruleProvider;
        _codeReviewService = codeReviewService;
    }

    /// <summary>
    /// The current wizard step.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
    [NotifyPropertyChangedFor(nameof(CurrentStepIndex))]
    [NotifyPropertyChangedFor(nameof(IsOnRepositoryStep))]
    [NotifyPropertyChangedFor(nameof(IsOnFilesStep))]
    [NotifyPropertyChangedFor(nameof(IsOnRulesStep))]
    [NotifyPropertyChangedFor(nameof(IsOnAnalysisStep))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private WizardStep _currentStep = WizardStep.Repository;

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
    /// The base branch to compare against.
    /// </summary>
    [ObservableProperty]
    private string _baseBranch = "main";

    /// <summary>
    /// Whether the wizard is currently fetching changes.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private bool _isFetchingChanges;

    /// <summary>
    /// Whether the wizard is currently running analysis.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private bool _isAnalyzing;

    /// <summary>
    /// The fetched diff (available after files step).
    /// </summary>
    [ObservableProperty]
    private GitDiff? _diff;

    /// <summary>
    /// Files available for selection.
    /// </summary>
    public ObservableCollection<SelectableFileViewModel> Files { get; } = [];

    /// <summary>
    /// Rules available for selection.
    /// </summary>
    public ObservableCollection<SelectableRuleViewModel> Rules { get; } = [];

    /// <summary>
    /// Hierarchical file nodes for the selection tree.
    /// </summary>
    public ObservableCollection<SelectableFileTreeNode> FileNodes { get; } = [];

    /// <summary>
    /// Filter text for file search.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredFileNodes))]
    private string _fileFilter = string.Empty;

    /// <summary>
    /// Total lines added across selected files.
    /// </summary>
    public int TotalLinesAdded => Files.Where(f => f.IsSelected).Sum(f => f.FileChange.LinesAdded);

    /// <summary>
    /// Total lines deleted across selected files.
    /// </summary>
    public int TotalLinesDeleted => Files.Where(f => f.IsSelected).Sum(f => f.FileChange.LinesDeleted);

    /// <summary>
    /// Filtered file nodes based on search text.
    /// </summary>
    public IEnumerable<SelectableFileTreeNode> FilteredFileNodes
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FileFilter))
                return FileNodes;

            // Filter to nodes that match or have children that match
            return FileNodes.SelectMany(FilterNode).ToList();
        }
    }

    private IEnumerable<SelectableFileTreeNode> FilterNode(SelectableFileTreeNode node)
    {
        // If this node matches, include it
        if (node.Name.Contains(FileFilter, StringComparison.OrdinalIgnoreCase))
        {
            yield return node;
            yield break;
        }

        // Check children
        var matchingChildren = node.Children.SelectMany(FilterNode).ToList();
        if (matchingChildren.Count > 0)
        {
            // Create a filtered copy with only matching children
            var filteredNode = new SelectableFileTreeNode(node.Name, node.FullPath, node.IsFile, node.FileViewModel)
            {
                Parent = node.Parent
            };
            foreach (var child in matchingChildren)
            {
                filteredNode.Children.Add(child);
            }
            yield return filteredNode;
        }
    }

    /// <summary>
    /// Error message if something went wrong.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    /// <summary>
    /// Whether there is an active error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Whether the wizard is busy with any asynchronous operation.
    /// </summary>
    public bool IsBusy => IsFetchingChanges || IsAnalyzing;

    /// <summary>
    /// The current step index (1-based for UI).
    /// </summary>
    public int CurrentStepIndex => (int)CurrentStep + 1;

    // Step visibility properties
    public bool IsOnRepositoryStep => CurrentStep == WizardStep.Repository;
    public bool IsOnFilesStep => CurrentStep == WizardStep.Files;
    public bool IsOnRulesStep => CurrentStep == WizardStep.Rules;
    public bool IsOnAnalysisStep => CurrentStep == WizardStep.Analysis;

    /// <summary>
    /// Whether back navigation is allowed.
    /// </summary>
    public bool CanGoBack => CurrentStep > WizardStep.Repository && !IsAnalyzing;

    /// <summary>
    /// Whether next/run navigation is allowed.
    /// </summary>
    public bool CanGoNext => CurrentStep switch
    {
        WizardStep.Repository => HasRepository && !IsFetchingChanges,
        WizardStep.Files => Files.Any(f => f.IsSelected) && !IsFetchingChanges,
        WizardStep.Rules => Rules.Any(r => r.IsSelected) && !IsAnalyzing,
        WizardStep.Analysis => false, // No next from analysis
        _ => false
    };

    /// <summary>
    /// Text for the next button.
    /// </summary>
    public string NextButtonText => CurrentStep switch
    {
        WizardStep.Repository => "Fetch Changes",
        WizardStep.Files => "Select Rules",
        WizardStep.Rules => "Run Analysis",
        WizardStep.Analysis => "Done",
        _ => "Next"
    };

    /// <summary>
    /// Number of selected files.
    /// </summary>
    public int SelectedFileCount => Files.Count(f => f.IsSelected);

    /// <summary>
    /// Number of selected rules.
    /// </summary>
    public int SelectedRuleCount => Rules.Count(r => r.IsSelected);

    /// <summary>
    /// Navigate back one step.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (CurrentStep > WizardStep.Repository)
        {
            CurrentStep = CurrentStep - 1;
        }
    }

    /// <summary>
    /// Navigate forward or execute action for current step.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        try
        {
            switch (CurrentStep)
            {
                case WizardStep.Repository:
                    await FetchChangesAsync(cancellationToken);
                    break;

                case WizardStep.Files:
                    LoadRulesForSelectedFiles();
                    CurrentStep = WizardStep.Rules;
                    break;

                case WizardStep.Rules:
                    CurrentStep = WizardStep.Analysis;
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>
    /// Fetches git changes and populates the file list.
    /// </summary>
    private async Task FetchChangesAsync(CancellationToken cancellationToken)
    {
        if (!HasRepository || RepositoryPath == null) return;

        try
        {
            IsFetchingChanges = true;
            Files.Clear();

            var diff = await _gitService.GetDiffFromBaseAsync(RepositoryPath, BaseBranch, cancellationToken);
            Diff = diff;

            foreach (var file in diff.Files.OrderBy(f => f.Path))
            {
                var fileVm = new SelectableFileViewModel(file);
                fileVm.PropertyChanged += OnFileViewModelPropertyChanged;
                Files.Add(fileVm);
            }

            BuildFileTree();

            if (diff.Files.Count == 0)
            {
                ErrorMessage = $"No changes found between {CurrentBranch} and {BaseBranch}";
            }
            else
            {
                CurrentStep = WizardStep.Files;
            }

            OnPropertyChanged(nameof(SelectedFileCount));
        }
        finally
        {
            IsFetchingChanges = false;
        }
    }

    private void OnFileViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableFileViewModel.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedFileCount));
            OnPropertyChanged(nameof(CanGoNext));
            NextCommand.NotifyCanExecuteChanged();
        }
    }

    private void BuildFileTree()
    {
        FileNodes.Clear();
        var folderNodes = new Dictionary<string, SelectableFileTreeNode>();

        foreach (var file in Files)
        {
            var parts = file.Path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";
            SelectableFileTreeNode? parent = null;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";
                var isFile = i == parts.Length - 1;

                if (!folderNodes.TryGetValue(currentPath, out var node))
                {
                    node = new SelectableFileTreeNode(part, currentPath, isFile, isFile ? file : null);
                    folderNodes[currentPath] = node;

                    if (parent == null)
                    {
                        FileNodes.Add(node);
                    }
                    else
                    {
                        node.Parent = parent;
                        parent.Children.Add(node);
                    }
                }
                parent = node;
            }
        }
    }

    /// <summary>
    /// Loads rules applicable to the selected files' languages.
    /// </summary>
    private void LoadRulesForSelectedFiles()
    {
        // Unsubscribe from existing rules
        foreach (var rule in Rules)
        {
            rule.PropertyChanged -= OnRulePropertyChanged;
        }
        
        Rules.Clear();

        // Get unique languages from selected files
        var languages = Files
            .Where(f => f.IsSelected && !string.IsNullOrEmpty(f.Language))
            .Select(f => f.Language!)
            .Distinct()
            .ToHashSet();

        // Load rules for those languages
        var allRules = _ruleProvider.GetAllRules()
            .Where(r => languages.Contains(r.Language, StringComparer.OrdinalIgnoreCase))
            .OrderBy(r => r.Language)
            .ThenBy(r => r.Category)
            .ThenBy(r => r.Name);

        foreach (var rule in allRules)
        {
            var selectableRule = new SelectableRuleViewModel(rule);
            selectableRule.PropertyChanged += OnRulePropertyChanged;
            Rules.Add(selectableRule);
        }

        OnPropertyChanged(nameof(SelectedRuleCount));
        OnPropertyChanged(nameof(CanGoNext));
        NextCommand.NotifyCanExecuteChanged();
    }

    private void OnRulePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableRuleViewModel.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedRuleCount));
            OnPropertyChanged(nameof(CanGoNext));
            NextCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Select all files.
    /// </summary>
    [RelayCommand]
    private void SelectAllFiles()
    {
        foreach (var file in Files)
        {
            file.IsSelected = true;
        }
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(TotalLinesAdded));
        OnPropertyChanged(nameof(TotalLinesDeleted));
        OnPropertyChanged(nameof(CanGoNext));
        NextCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Deselect all files.
    /// </summary>
    [RelayCommand]
    private void DeselectAllFiles()
    {
        foreach (var file in Files)
        {
            file.IsSelected = false;
        }
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(TotalLinesAdded));
        OnPropertyChanged(nameof(TotalLinesDeleted));
        OnPropertyChanged(nameof(CanGoNext));
        NextCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Invert file selection.
    /// </summary>
    [RelayCommand]
    private void InvertFileSelection()
    {
        foreach (var file in Files)
        {
            file.IsSelected = !file.IsSelected;
        }
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(TotalLinesAdded));
        OnPropertyChanged(nameof(TotalLinesDeleted));
        OnPropertyChanged(nameof(CanGoNext));
        NextCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Select all rules.
    /// </summary>
    [RelayCommand]
    private void SelectAllRules()
    {
        foreach (var rule in Rules)
        {
            rule.IsSelected = true;
        }
        OnPropertyChanged(nameof(SelectedRuleCount));
        OnPropertyChanged(nameof(CanGoNext));
        NextCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Deselect all rules.
    /// </summary>
    [RelayCommand]
    private void DeselectAllRules()
    {
        foreach (var rule in Rules)
        {
            rule.IsSelected = false;
        }
        OnPropertyChanged(nameof(SelectedRuleCount));
        OnPropertyChanged(nameof(CanGoNext));
        NextCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Gets the selected files as FileChange objects.
    /// </summary>
    public IEnumerable<FileChange> GetSelectedFiles() =>
        Files.Where(f => f.IsSelected).Select(f => f.FileChange);

    /// <summary>
    /// Gets the selected rule IDs.
    /// </summary>
    public IEnumerable<string> GetSelectedRuleIds() =>
        Rules.Where(r => r.IsSelected).Select(r => r.Id);

    /// <summary>
    /// Creates a filtered GitDiff containing only selected files.
    /// </summary>
    public GitDiff? GetFilteredDiff()
    {
        if (Diff == null) return null;

        var selectedFiles = GetSelectedFiles().ToList();

        return new GitDiff
        {
            BaseRef = Diff.BaseRef,
            HeadRef = Diff.HeadRef,
            Files = selectedFiles
        };
    }

    /// <summary>
    /// Refreshes properties when repository changes.
    /// </summary>
    public void RefreshRepositoryState()
    {
        OnPropertyChanged(nameof(HasRepository));
        OnPropertyChanged(nameof(RepositoryPath));
        OnPropertyChanged(nameof(RepositoryDisplayName));
        OnPropertyChanged(nameof(CurrentBranch));
        OnPropertyChanged(nameof(CanGoNext));
        NextCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Resets the wizard to the initial state.
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        CurrentStep = WizardStep.Repository;
        Files.Clear();
        Rules.Clear();
        Diff = null;
        ErrorMessage = null;
        IsAnalyzing = false;
        IsFetchingChanges = false;
        FileNodes.Clear();
    }
}
