namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Analysis page handling code review execution and results display.
/// </summary>
public partial class AnalysisViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly ICodeReviewService _codeReviewService;
    private readonly IGitService _gitService;
    private readonly IRuleProvider _ruleProvider;

    public AnalysisViewModel(
        MainViewModel mainViewModel, 
        ICodeReviewService codeReviewService,
        IGitService gitService,
        IRuleProvider ruleProvider)
    {
        _mainViewModel = mainViewModel;
        _codeReviewService = codeReviewService;
        _gitService = gitService;
        _ruleProvider = ruleProvider;
    }

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
    /// Whether an analysis is currently running.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRunAnalysis))]
    [NotifyCanExecuteChangedFor(nameof(RunAnalysisCommand))]
    private bool _isAnalyzing;

    /// <summary>
    /// Progress message during analysis.
    /// </summary>
    [ObservableProperty]
    private string _progressMessage = string.Empty;

    /// <summary>
    /// The most recent review result.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    [NotifyPropertyChangedFor(nameof(CriticalCount))]
    [NotifyPropertyChangedFor(nameof(ErrorCount))]
    [NotifyPropertyChangedFor(nameof(WarningCount))]
    [NotifyPropertyChangedFor(nameof(InfoCount))]
    [NotifyPropertyChangedFor(nameof(TotalIssues))]
    private ReviewResult? _currentResult;

    /// <summary>
    /// Whether there are results to display.
    /// </summary>
    public bool HasResults => CurrentResult != null;

    /// <summary>
    /// Whether analysis can be run.
    /// </summary>
    public bool CanRunAnalysis => HasRepository && !IsAnalyzing;

    /// <summary>
    /// Number of critical issues.
    /// </summary>
    public int CriticalCount => CurrentResult?.CriticalCount ?? 0;

    /// <summary>
    /// Number of error issues.
    /// </summary>
    public int ErrorCount => CurrentResult?.ErrorCount ?? 0;

    /// <summary>
    /// Number of warning issues.
    /// </summary>
    public int WarningCount => CurrentResult?.WarningCount ?? 0;

    /// <summary>
    /// Number of info issues.
    /// </summary>
    public int InfoCount => CurrentResult?.InfoCount ?? 0;

    /// <summary>
    /// Total number of issues.
    /// </summary>
    public int TotalIssues => CurrentResult?.Issues.Count ?? 0;

    /// <summary>
    /// Issues grouped by file for display.
    /// </summary>
    public ObservableCollection<FileIssueGroup> GroupedIssues { get; } = [];

    /// <summary>
    /// Analysis log messages for visibility.
    /// </summary>
    public ObservableCollection<AnalysisLogEntry> AnalysisLog { get; } = [];

    /// <summary>
    /// Root nodes for the file tree sidebar.
    /// </summary>
    public ObservableCollection<FileTreeNode> FileTreeNodes { get; } = [];

    /// <summary>
    /// Number of files found in the diff.
    /// </summary>
    [ObservableProperty]
    private int _filesInDiff;

    /// <summary>
    /// Number of rules being applied.
    /// </summary>
    [ObservableProperty]
    private int _rulesApplied;

    /// <summary>
    /// Whether to show detailed log.
    /// </summary>
    [ObservableProperty]
    private bool _showDetailedLog = true;

    /// <summary>
    /// Current progress value (0 to ProgressMaximum).
    /// </summary>
    [ObservableProperty]
    private double _progressValue;

    /// <summary>
    /// Maximum progress value (total work items).
    /// </summary>
    [ObservableProperty]
    private double _progressMaximum = 100;

    /// <summary>
    /// Name of the file currently being analyzed.
    /// </summary>
    [ObservableProperty]
    private string _currentFileName = string.Empty;

    /// <summary>
    /// ID of the rule currently being applied.
    /// </summary>
    [ObservableProperty]
    private string _currentRuleId = string.Empty;

    /// <summary>
    /// Number of completed work items (rule applications).
    /// </summary>
    [ObservableProperty]
    private int _completedWorkItems;

    /// <summary>
    /// Total number of work items (rule applications).
    /// </summary>
    [ObservableProperty]
    private int _totalWorkItems;

    /// <summary>
    /// Currently selected severity filter.
    /// </summary>
    [ObservableProperty]
    private Severity? _severityFilter;

    /// <summary>
    /// Currently selected file filter.
    /// </summary>
    [ObservableProperty]
    private string? _fileFilter;

    /// <summary>
    /// Filtered issues for display.
    /// </summary>
    public IEnumerable<Issue> FilteredIssues
    {
        get
        {
            if (CurrentResult == null) return [];
            
            var issues = CurrentResult.Issues.AsEnumerable();
            
            if (SeverityFilter.HasValue)
            {
                issues = issues.Where(i => i.Severity == SeverityFilter.Value);
            }
            
            if (!string.IsNullOrEmpty(FileFilter))
            {
                issues = issues.Where(i => i.FilePath.Contains(FileFilter, StringComparison.OrdinalIgnoreCase));
            }
            
            return issues.OrderByDescending(i => i.Severity).ThenBy(i => i.FilePath).ThenBy(i => i.StartLine);
        }
    }

    /// <summary>
    /// Runs the code review analysis.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRunAnalysis))]
    private async Task RunAnalysisAsync(CancellationToken cancellationToken)
    {
        if (!HasRepository || RepositoryPath == null) return;

        try
        {
            IsAnalyzing = true;
            AnalysisLog.Clear();
            GroupedIssues.Clear();
            FilesInDiff = 0;
            RulesApplied = 0;

            AddLog("Starting analysis...", AnalysisLogLevel.Info);
            AddLog($"Repository: {RepositoryPath}", AnalysisLogLevel.Info);
            AddLog($"Current branch: {CurrentBranch}", AnalysisLogLevel.Info);
            AddLog($"Comparing against: {BaseBranch}", AnalysisLogLevel.Info);

            // Get the diff first to show what files are being analyzed
            ProgressMessage = "Getting git diff...";
            AddLog("Fetching git diff...", AnalysisLogLevel.Info);
            
            var diff = await _gitService.GetDiffFromBaseAsync(RepositoryPath, BaseBranch, cancellationToken);
            FilesInDiff = diff.Files.Count;

            if (diff.Files.Count == 0)
            {
                AddLog($"No file changes found between {CurrentBranch} and {BaseBranch}", AnalysisLogLevel.Warning);
                AddLog("Make sure you have uncommitted changes or commits ahead of the base branch", AnalysisLogLevel.Info);
                ProgressMessage = $"No changes found between {CurrentBranch} and {BaseBranch}";
                
                CurrentResult = new ReviewResult
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    StartedAt = DateTimeOffset.UtcNow,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Diff = diff,
                    Issues = [],
                    AppliedRules = [],
                    IsSuccess = true
                };
                return;
            }

            AddLog($"Found {diff.Files.Count} changed file(s):", AnalysisLogLevel.Success);
            foreach (var file in diff.Files)
            {
                AddLog($"  - {file.Path} ({file.ChangeType}, +{file.LinesAdded}/-{file.LinesDeleted})", AnalysisLogLevel.Info);
            }

            // Check rules
            var enabledRules = _ruleProvider.GetEnabledRules().ToList();
            RulesApplied = enabledRules.Count;
            
            if (enabledRules.Count == 0)
            {
                AddLog("No enabled rules found!", AnalysisLogLevel.Warning);
                AddLog("Check that rules are loaded in the Rules tab", AnalysisLogLevel.Info);
            }
            else
            {
                AddLog($"Applying {enabledRules.Count} rule(s)...", AnalysisLogLevel.Info);
                var rulesWithContent = enabledRules.Where(r => !string.IsNullOrEmpty(r.PromptContent)).ToList();
                if (rulesWithContent.Count < enabledRules.Count)
                {
                    AddLog($"Warning: {enabledRules.Count - rulesWithContent.Count} rule(s) have no prompt content", AnalysisLogLevel.Warning);
                }
            }

            ProgressMessage = "Running LLM analysis...";
            AddLog("Sending to LLM for analysis...", AnalysisLogLevel.Info);

            // Calculate total work items for progress tracking
            var totalWorkItems = diff.Files
                .Where(f => !string.IsNullOrEmpty(f.Language))
                .GroupBy(f => f.Language!)
                .Sum(g => g.Count() * _ruleProvider.GetRulesByLanguage(g.Key)
                    .Count(r => r.Enabled && !string.IsNullOrEmpty(r.PromptContent)));

            // Initialize progress tracking
            ProgressValue = 0;
            ProgressMaximum = totalWorkItems;
            TotalWorkItems = totalWorkItems;
            CompletedWorkItems = 0;
            CurrentFileName = string.Empty;
            CurrentRuleId = string.Empty;
            
            BuildFileTree(diff.Files);

            // Create progress reporter
            var progressReporter = new AnalysisProgressReporter(this);
            progressReporter.SetTotalWorkItems(totalWorkItems);
            progressReporter.SetFileTree(FileTreeNodes);

            var result = await _codeReviewService.AnalyzeDiffAsync(diff, progressReporter, cancellationToken);

            CurrentResult = result;
            UpdateGroupedIssues();

            if (result.IsSuccess)
            {
                AddLog($"Analysis complete in {result.Duration?.TotalSeconds:F1}s", AnalysisLogLevel.Success);
                AddLog($"Found {TotalIssues} issue(s): {CriticalCount} critical, {ErrorCount} errors, {WarningCount} warnings, {InfoCount} info", 
                    TotalIssues > 0 ? AnalysisLogLevel.Warning : AnalysisLogLevel.Success);
                ProgressMessage = $"Analysis complete. Found {TotalIssues} issue(s) in {result.Duration?.TotalSeconds:F1}s";
            }
            else
            {
                AddLog($"Analysis failed: {result.ErrorMessage}", AnalysisLogLevel.Error);
                
                // Provide helpful guidance for model errors
                if (result.ErrorMessage?.Contains("model", StringComparison.OrdinalIgnoreCase) == true ||
                    result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    AddLog("This appears to be a model configuration issue.", AnalysisLogLevel.Warning);
                    AddLog("Please check your AI provider settings and verify the model name is correct.", AnalysisLogLevel.Info);
                    AddLog("Use the 'Test Connection' button in Settings to verify your configuration.", AnalysisLogLevel.Info);
                }
                
                ProgressMessage = $"Analysis failed: {result.ErrorMessage}";
            }
        }
        catch (OperationCanceledException)
        {
            AddLog("Analysis cancelled by user", AnalysisLogLevel.Warning);
            ProgressMessage = "Analysis cancelled.";
        }
        catch (Exception ex)
        {
            AddLog($"Error: {ex.Message}", AnalysisLogLevel.Error);
            ProgressMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    /// <summary>
    /// Adds a log entry to the analysis log. Public for use by AnalysisProgressReporter.
    /// </summary>
    public void AddLog(string message, AnalysisLogLevel level)
    {
        AnalysisLog.Add(new AnalysisLogEntry
        {
            Timestamp = DateTime.Now,
            Message = message,
            Level = level
        });
    }

    /// <summary>
    /// Adds a detailed log entry with LLM prompt/response data.
    /// </summary>
    public void AddLog(string message, AnalysisLogLevel level, string? filePath, string? ruleId, 
        string? llmPrompt = null, string? llmResponse = null)
    {
        AnalysisLog.Add(new AnalysisLogEntry
        {
            Timestamp = DateTime.Now,
            Message = message,
            Level = level,
            FilePath = filePath,
            RuleId = ruleId,
            LlmPrompt = llmPrompt,
            LlmResponse = llmResponse
        });
    }

    /// <summary>
    /// Clears the severity filter.
    /// </summary>
    [RelayCommand]
    private void ClearSeverityFilter()
    {
        SeverityFilter = null;
        OnPropertyChanged(nameof(FilteredIssues));
    }

    /// <summary>
    /// Sets the severity filter.
    /// </summary>
    [RelayCommand]
    private void SetSeverityFilter(Severity severity)
    {
        SeverityFilter = severity;
        OnPropertyChanged(nameof(FilteredIssues));
    }

    /// <summary>
    /// Clears the file filter.
    /// </summary>
    [RelayCommand]
    private void ClearFileFilter()
    {
        FileFilter = null;
        OnPropertyChanged(nameof(FilteredIssues));
    }

    private void UpdateGroupedIssues()
    {
        GroupedIssues.Clear();
        
        if (CurrentResult == null) return;

        var groups = CurrentResult.Issues
            .GroupBy(i => i.FilePath)
            .OrderByDescending(g => g.Max(i => (int)i.Severity))
            .ThenBy(g => g.Key);

        foreach (var group in groups)
        {
            var issues = group.OrderByDescending(i => i.Severity).ThenBy(i => i.StartLine).ToList();
            GroupedIssues.Add(new FileIssueGroup
            {
                FilePath = group.Key,
                FileName = Path.GetFileName(group.Key),
                Issues = issues
            });

            // Update the file tree node with the issue count
            var node = FindFileNode(group.Key);
            if (node != null)
            {
                node.IssueCount = issues.Count;
            }
        }
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
        OnPropertyChanged(nameof(CanRunAnalysis));
        RunAnalysisCommand.NotifyCanExecuteChanged();
    }

    private void BuildFileTree(IEnumerable<FileChange> files)
    {
        FileTreeNodes.Clear();
        var folderMap = new Dictionary<string, FileTreeNode>();

        foreach (var file in files.OrderBy(f => f.Path))
        {
            var parts = file.Path.Split('/', '\\');
            var currentPath = "";
            FileTreeNode? parent = null;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";
                var isLast = i == parts.Length - 1;

                if (!folderMap.TryGetValue(currentPath, out var node))
                {
                    node = new FileTreeNode
                    {
                        Name = part,
                        FullPath = currentPath,
                        IsFile = isLast,
                        Parent = parent,
                        Status = FileAnalysisStatus.Pending,
                        ChangeType = isLast ? file.ChangeType : FileChangeType.Modified,
                        LinesAdded = isLast ? file.LinesAdded : 0,
                        LinesDeleted = isLast ? file.LinesDeleted : 0
                    };
                    folderMap[currentPath] = node;

                    if (parent == null)
                    {
                        FileTreeNodes.Add(node);
                    }
                    else
                    {
                        parent.Children.Add(node);
                    }
                }
                parent = node;
            }
        }
    }

    public FileTreeNode? FindFileNode(string filePath)
    {
        // Simple search in the map-like structure would be better, 
        // but we can just traverse the tree or use a flattened approach if needed.
        // Given BuildFileTree just ran, we could have shared the map, 
        // but for now let's just do a recursive search.
        return FindFileNodeRecursive(FileTreeNodes, filePath);
    }

    private FileTreeNode? FindFileNodeRecursive(IEnumerable<FileTreeNode> nodes, string filePath)
    {
        foreach (var node in nodes)
        {
            if (node.IsFile && (node.FullPath == filePath || node.FullPath.Replace('\\', '/') == filePath.Replace('\\', '/')))
            {
                return node;
            }
            
            var found = FindFileNodeRecursive(node.Children, filePath);
            if (found != null) return found;
        }
        return null;
    }
}

/// <summary>
/// Represents a group of issues for a single file.
/// </summary>
public class FileIssueGroup
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required List<Issue> Issues { get; init; }
    public int CriticalCount => Issues.Count(i => i.Severity == Severity.Critical);
    public int ErrorCount => Issues.Count(i => i.Severity == Severity.Error);
    public int WarningCount => Issues.Count(i => i.Severity == Severity.Warning);
    public int InfoCount => Issues.Count(i => i.Severity == Severity.Info);
    public Severity MaxSeverity => Issues.Count > 0 ? Issues.Max(i => i.Severity) : Severity.Info;
}

/// <summary>
/// Represents a log entry from the analysis process.
/// </summary>
public class AnalysisLogEntry
{
    public required DateTime Timestamp { get; init; }
    public required string Message { get; init; }
    public required AnalysisLogLevel Level { get; init; }
    
    // Expandable detail properties
    public string? LlmPrompt { get; init; }
    public string? LlmResponse { get; init; }
    public string? FilePath { get; init; }
    public string? RuleId { get; init; }
    public int? LinesAdded { get; init; }
    public int? LinesDeleted { get; init; }
    
    /// <summary>
    /// Whether this entry has expandable details.
    /// </summary>
    public bool HasDetails => !string.IsNullOrEmpty(LlmPrompt) || !string.IsNullOrEmpty(LlmResponse);
}

/// <summary>
/// Log level for analysis entries.
/// </summary>
public enum AnalysisLogLevel
{
    Info,
    Success,
    Warning,
    Error
}
