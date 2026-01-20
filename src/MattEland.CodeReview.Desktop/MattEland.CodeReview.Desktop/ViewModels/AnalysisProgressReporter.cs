using MattEland.CodeReview.Core.Analysis;
using MattEland.CodeReview.Desktop.ViewModels;

namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// Progress reporter that logs to the AnalysisViewModel's log.
/// </summary>
public sealed class AnalysisProgressReporter : IAnalysisProgressReporter
{
    private readonly AnalysisViewModel _viewModel;
    private int _totalWorkItems;
    private int _completedWorkItems;
    private ObservableCollection<FileTreeNode>? _fileTree;
    private FileTreeNode? _currentFileNode;
    
    // Track pending requests to pair with responses
    private readonly Dictionary<string, (string FilePath, string RuleId, string Request)> _pendingRequests = new();

    public AnalysisProgressReporter(AnalysisViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void SetTotalWorkItems(int total)
    {
        _totalWorkItems = total;
        _completedWorkItems = 0;
    }

    public void SetFileTree(ObservableCollection<FileTreeNode> fileTree)
    {
        _fileTree = fileTree;
    }

    public void ReportProgress(int currentFile, int totalFiles, int currentRule, int totalRules, string filePath, string ruleId)
    {
        _completedWorkItems++;
        
        // Update ViewModel properties
        _viewModel.ProgressValue = _completedWorkItems;
        _viewModel.CompletedWorkItems = _completedWorkItems;
        _viewModel.CurrentFileName = Path.GetFileName(filePath);
        _viewModel.CurrentRuleId = ruleId;

        // Update File Node Status
        if (_currentFileNode != null && (_currentFileNode.FullPath != filePath && _currentFileNode.FullPath.Replace('\\', '/') != filePath.Replace('\\', '/')))
        {
            _currentFileNode.Status = FileAnalysisStatus.Finished;
        }

        if (_currentFileNode == null || (_currentFileNode.FullPath != filePath && _currentFileNode.FullPath.Replace('\\', '/') != filePath.Replace('\\', '/')))
        {
            _currentFileNode = _viewModel.FindFileNode(filePath);
            if (_currentFileNode != null)
            {
                _currentFileNode.Status = FileAnalysisStatus.Running;
            }
        }

        var progressPercent = _totalWorkItems > 0 
            ? (int)((_completedWorkItems / (double)_totalWorkItems) * 100) 
            : 0;
        
        var fileName = Path.GetFileName(filePath);
        var remaining = _totalWorkItems - _completedWorkItems;
        _viewModel.AddLog(
            $"[{progressPercent}%] Analyzing {fileName} with rule {ruleId} ({remaining} remaining)",
            AnalysisLogLevel.Info);
        
        // Mark as finished if this is the very last work item
        if (_completedWorkItems >= _totalWorkItems && _currentFileNode != null)
        {
            _currentFileNode.Status = FileAnalysisStatus.Finished;
        }
    }

    public void ReportLlmRequest(string filePath, string ruleId, string request)
    {
        // Store the request to pair with the response later
        var key = $"{filePath}|{ruleId}";
        _pendingRequests[key] = (filePath, ruleId, request);
        
        var fileName = Path.GetFileName(filePath);
        _viewModel.AddLog($"→ LLM Request for {fileName} ({ruleId})", AnalysisLogLevel.Info);
    }

    public void ReportLlmResponse(string filePath, string ruleId, string response)
    {
        var fileName = Path.GetFileName(filePath);
        var key = $"{filePath}|{ruleId}";
        
        // Try to get the paired request
        string? request = null;
        if (_pendingRequests.TryGetValue(key, out var pendingRequest))
        {
            request = pendingRequest.Request;
            _pendingRequests.Remove(key);
        }
        
        // Create a detailed log entry with both request and response
        _viewModel.AddLog(
            $"← LLM Response for {fileName} ({ruleId})",
            AnalysisLogLevel.Success,
            filePath,
            ruleId,
            request,
            response);
    }

    public void ReportJsonParsingError(string filePath, string ruleId, string error, string response)
    {
        var fileName = Path.GetFileName(filePath);
        var key = $"{filePath}|{ruleId}";
        
        // Try to get the paired request
        string? request = null;
        if (_pendingRequests.TryGetValue(key, out var pendingRequest))
        {
            request = pendingRequest.Request;
            _pendingRequests.Remove(key);
        }
        
        _viewModel.AddLog(
            $"✗ JSON Parsing Error for {fileName} ({ruleId}): {error}",
            AnalysisLogLevel.Error,
            filePath,
            ruleId,
            request,
            response);
    }
}
