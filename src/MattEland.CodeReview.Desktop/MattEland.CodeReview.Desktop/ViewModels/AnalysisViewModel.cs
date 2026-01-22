using System.ComponentModel;

namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Analysis page handling code review execution and results display.
/// </summary>
[Bindable(true)]
public partial class AnalysisViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly ICodeReviewService _codeReviewService;
    private readonly IGitService _gitService;
    private readonly IProfileProvider _profileProvider;
    private CancellationTokenSource? _analysisCts;

    public AnalysisViewModel(
        MainViewModel mainViewModel, 
        ICodeReviewService codeReviewService,
        IGitService gitService,
        IProfileProvider profileProvider)
    {
        _mainViewModel = mainViewModel;
        _codeReviewService = codeReviewService;
        _gitService = gitService;
        _profileProvider = profileProvider;
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
    [NotifyPropertyChangedFor(nameof(CanCancelAnalysis))]
    [NotifyCanExecuteChangedFor(nameof(RunAnalysisCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelAnalysisCommand))]
    private bool _isAnalyzing;

    /// <summary>
    /// Whether analysis can be cancelled.
    /// </summary>
    public bool CanCancelAnalysis => IsAnalyzing;

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
    /// Number of profiles being applied.
    /// </summary>
    [ObservableProperty]
    private int _profilesApplied;

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
    /// ID of the profile currently being applied.
    /// </summary>
    [ObservableProperty]
    private string _currentProfileId = string.Empty;

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
    /// Current batch index (1-based).
    /// </summary>
    [ObservableProperty]
    private int _currentBatchIndex;

    /// <summary>
    /// Total number of batches.
    /// </summary>
    [ObservableProperty]
    private int _totalBatches;

    /// <summary>
    /// Reason why the current batch files are grouped together.
    /// </summary>
    [ObservableProperty]
    private string _currentBatchGroupingReason = string.Empty;

    /// <summary>
    /// Files in the current batch being analyzed.
    /// </summary>
    public ObservableCollection<string> CurrentBatchFiles { get; } = [];

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
    /// How issues are grouped in the results view.
    /// </summary>
    [ObservableProperty]
    private IssueGroupingMode _groupingMode = IssueGroupingMode.ByFile;

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
         await RunAnalysisAsync(null, null, cancellationToken);
    }

    /// <summary>
    /// Cancels the running analysis.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancelAnalysis))]
    private void CancelAnalysis()
    {
        _analysisCts?.Cancel();
        AddLog("Cancellation requested...", AnalysisLogLevel.Warning);
        ProgressMessage = "Cancelling analysis...";
    }

    /// <summary>
    /// Runs the code review analysis with an optional filtered diff and profile set.
    /// </summary>
    public async Task RunAnalysisAsync(GitDiff? selectedDiff, IEnumerable<string>? profileIds, CancellationToken cancellationToken)
    {
        if (!HasRepository || RepositoryPath == null) return;

        // Create linked CTS so we can cancel internally while respecting external cancellation
        _analysisCts?.Dispose();
        _analysisCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _analysisCts.Token;

        try
        {
            IsAnalyzing = true;
            AnalysisLog.Clear();
            GroupedIssues.Clear();
            FilesInDiff = 0;
            ProfilesApplied = 0;

            AddLog("Starting analysis...", AnalysisLogLevel.Info);
            AddLog($"Repository: {RepositoryPath}", AnalysisLogLevel.Info);
            AddLog($"Current branch: {CurrentBranch}", AnalysisLogLevel.Info);
            AddLog($"Comparing against: {BaseBranch}", AnalysisLogLevel.Info);

            GitDiff diff;
            if (selectedDiff != null)
            {
                diff = selectedDiff;
                AddLog("Using selected files for analysis", AnalysisLogLevel.Info);
            }
            else
            {
                // Get the diff first to show what files are being analyzed
                ProgressMessage = "Getting git diff...";
                AddLog("Fetching git diff...", AnalysisLogLevel.Info);
                diff = await _gitService.GetDiffFromBaseAsync(RepositoryPath, BaseBranch, cancellationToken);
            }
            
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
                    AppliedProfiles = [],
                    IsSuccess = true
                };
                return;
            }

            AddLog($"Found {diff.Files.Count} changed file(s):", AnalysisLogLevel.Success);
            foreach (var file in diff.Files)
            {
                AddLog($"  - {file.Path} ({file.ChangeType}, +{file.LinesAdded}/-{file.LinesDeleted})", AnalysisLogLevel.Info);
            }

            // Check profiles
            var allProfiles = profileIds != null 
                ? profileIds.Select(id => _profileProvider.GetProfile(id)).Where(p => p != null).Cast<ReviewProfile>().ToList()
                : _profileProvider.GetEnabledProfiles().ToList();

            ProfilesApplied = allProfiles.Count;
            
            if (allProfiles.Count == 0)
            {
                AddLog("No profiles selected for analysis!", AnalysisLogLevel.Warning);
                AddLog("Please select at least one reviewer profile", AnalysisLogLevel.Info);
            }
            else
            {
                AddLog($"Using {allProfiles.Count} reviewer profile(s)...", AnalysisLogLevel.Info);
            }

            ProgressMessage = "Running LLM analysis...";
            AddLog("Sending to LLM for analysis...", AnalysisLogLevel.Info);

            // Calculate total work items for progress tracking
            var totalWorkItems = allProfiles.Count > 0 ? diff.Files.Count : 0;

            // Initialize progress tracking
            ProgressValue = 0;
            ProgressMaximum = totalWorkItems;
            TotalWorkItems = totalWorkItems;
            CompletedWorkItems = 0;
            CurrentFileName = string.Empty;
            CurrentProfileId = string.Empty;
            
            // Initialize empty result if needed so we can start adding issues
            CurrentResult = new ReviewResult
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                StartedAt = DateTimeOffset.UtcNow,
                CompletedAt = DateTimeOffset.UtcNow,
                Diff = diff,
                Issues = new List<Issue>(),
                AppliedProfiles = new List<ReviewProfile>(),
                IsSuccess = true
            };
            
            BuildFileTree(diff.Files);

            // Create progress reporter
            var progressReporter = new AnalysisProgressReporter(this);
            progressReporter.SetTotalWorkItems(totalWorkItems);
            progressReporter.SetFileTree(FileTreeNodes);

            var result = await _codeReviewService.AnalyzeDiffAsync(diff, profileIds, progressReporter, token);

            CurrentResult = result;
            UpdateGroupedIssues();

            if (result.IsSuccess)
            {
                AddLog($"Analysis complete in {result.Duration?.TotalSeconds:F1}s", AnalysisLogLevel.Success);
                AddLog($"Found {TotalIssues} issue(s): {CriticalCount} critical, {ErrorCount} errors, {WarningCount} warnings, {InfoCount} info", 
                    TotalIssues > 0 ? AnalysisLogLevel.Warning : AnalysisLogLevel.Success);
                ProgressMessage = $"Analysis complete. Found {TotalIssues} issue(s) in {result.Duration?.TotalSeconds:F1}s";
                
                // Automatically switch to Results tab if we found issues
                SelectedTabIndex = 1;
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
            ProfileId = ruleId,
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
        UpdateGroupedIssues();
    }

    /// <summary>
    /// Sets the severity filter.
    /// </summary>
    [RelayCommand]
    private void SetSeverityFilter(Severity severity)
    {
        SeverityFilter = severity;
        OnPropertyChanged(nameof(FilteredIssues));
        UpdateGroupedIssues();
    }

    /// <summary>
    /// Clears the file filter.
    /// </summary>
    [RelayCommand]
    private void ClearFileFilter()
    {
        FileFilter = null;
        OnPropertyChanged(nameof(FilteredIssues));
        UpdateGroupedIssues();
    }

    /// <summary>
    /// Sets grouping to by file.
    /// </summary>
    [RelayCommand]
    private void GroupByFile()
    {
        GroupingMode = IssueGroupingMode.ByFile;
        UpdateGroupedIssues();
    }

    /// <summary>
    /// Sets grouping to by severity.
    /// </summary>
    [RelayCommand]
    private void GroupBySeverity()
    {
        GroupingMode = IssueGroupingMode.BySeverity;
        UpdateGroupedIssues();
    }

    /// <summary>
    /// Index of the selected tab (0 = Analysis, 1 = Results).
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }
    private int _selectedTabIndex;

    private void UpdateGroupedIssues()
    {
        GroupedIssues.Clear();
        
        if (CurrentResult == null) return;

        // Apply filters
        var issues = CurrentResult.Issues.AsEnumerable();
        
        if (SeverityFilter.HasValue)
        {
            issues = issues.Where(i => i.Severity == SeverityFilter.Value);
        }
        
        if (!string.IsNullOrEmpty(FileFilter))
        {
            issues = issues.Where(i => i.FilePath.Contains(FileFilter, StringComparison.OrdinalIgnoreCase));
        }

        var issueList = issues.ToList();

        if (GroupingMode == IssueGroupingMode.BySeverity)
        {
            // Group by severity
            var groups = issueList
                .GroupBy(i => i.Severity)
                .OrderByDescending(g => (int)g.Key);

            foreach (var group in groups)
            {
                var groupIssues = group.OrderBy(i => i.FilePath).ThenBy(i => i.StartLine).ToList();
                GroupedIssues.Add(new FileIssueGroup
                {
                    FilePath = group.Key.ToString(),
                    FileName = GetSeverityLabel(group.Key),
                    Issues = groupIssues
                });
            }
        }
        else
        {
            // Group by file (default)
            var groups = issueList
                .GroupBy(i => i.FilePath)
                .OrderByDescending(g => g.Max(i => (int)i.Severity))
                .ThenBy(g => g.Key);

            foreach (var group in groups)
            {
                var groupIssues = group.OrderByDescending(i => i.Severity).ThenBy(i => i.StartLine).ToList();
                GroupedIssues.Add(new FileIssueGroup
                {
                    FilePath = group.Key,
                    FileName = Path.GetFileName(group.Key),
                    Issues = groupIssues
                });

                // Update the file tree node with the issue count
                var node = FindFileNode(group.Key);
                if (node != null)
                {
                    node.IssueCount = groupIssues.Count;
                }
            }
        }
    }

    private static string GetSeverityLabel(Severity severity) => severity switch
    {
        Severity.Critical => "🔴 Critical",
        Severity.Error => "🟠 Error",
        Severity.Warning => "🟢 Warning",
        _ => "ℹ️ Info"
    };

    /// <summary>
    /// Adds newly found issues to the results and updates the UI.
    /// </summary>
    public void AddIssues(IEnumerable<Issue> issues)
    {
        var issueList = issues.ToList();
        if (issueList.Count == 0) return;

        var result = CurrentResult ?? new ReviewResult
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            Diff = new GitDiff { BaseRef = "unknown", HeadRef = "unknown", Files = new List<FileChange>() },
            Issues = new List<Issue>(),
            AppliedProfiles = new List<ReviewProfile>(),
            IsSuccess = true
        };

        // Add to main result
        var allIssues = result.Issues.ToList();
        allIssues.AddRange(issueList);
        
        // Update the result object
        CurrentResult = new ReviewResult
        {
            Id = result.Id,
            StartedAt = result.StartedAt,
            CompletedAt = result.CompletedAt,
            Diff = result.Diff,
            Issues = allIssues,
            AppliedProfiles = result.AppliedProfiles,
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.ErrorMessage
        };

        // Update grouped issues for UI
        foreach (var group in issueList.GroupBy(i => i.FilePath))
        {
            var existingGroup = GroupedIssues.FirstOrDefault(g => g.FilePath == group.Key);
            if (existingGroup != null)
            {
                existingGroup.Issues.AddRange(group);
                // Trigger update for counts
                var index = GroupedIssues.IndexOf(existingGroup);
                if (index != -1)
                {
                    // Re-create the group to trigger UI updates for counts
                    // This is a bit inefficient but ensures the UI refreshes
                    // A better observable model for FileIssueGroup would be ideal
                    var newGroup = new FileIssueGroup
                    {
                        FilePath = existingGroup.FilePath,
                        FileName = existingGroup.FileName,
                        Issues = existingGroup.Issues.OrderByDescending(i => i.Severity).ThenBy(i => i.StartLine).ToList()
                    };
                    GroupedIssues[index] = newGroup;
                }
            }
            else
            {
                GroupedIssues.Add(new FileIssueGroup
                {
                    FilePath = group.Key,
                    FileName = Path.GetFileName(group.Key),
                    Issues = group.ToList()
                });
            }

            // Update file tree node
            var node = FindFileNode(group.Key);
            if (node != null)
            {
                node.IssueCount += group.Count();
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

    /// <summary>
    /// Requests navigation to the Home page.
    /// </summary>
    [RelayCommand]
    private void RequestNavigateToHome()
    {
        _mainViewModel.RequestNavigateToHome();
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
    public string? ProfileId { get; init; }
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

/// <summary>
/// How issues are grouped in the results view.
/// </summary>
public enum IssueGroupingMode
{
    /// <summary>Group issues by file path.</summary>
    ByFile,
    /// <summary>Group issues by severity level.</summary>
    BySeverity
}
