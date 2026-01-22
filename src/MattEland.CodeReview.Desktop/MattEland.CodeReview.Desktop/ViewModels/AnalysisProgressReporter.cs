using MattEland.CodeReview.Core.Analysis;
using MattEland.CodeReview.Desktop.ViewModels;
using MattEland.CodeReview.Core.Models;

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
    private readonly Dictionary<string, (string FilePath, string ProfileId, string Request)> _pendingRequests = new();

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

    public void ReportProgress(int currentFile, int totalFiles, int currentRule, int totalRules, string filePath, string profileId)
    {
        _completedWorkItems++;
        
        // Update ViewModel properties
        _viewModel.ProgressValue = _completedWorkItems;
        _viewModel.CompletedWorkItems = _completedWorkItems;
        _viewModel.CurrentFileName = Path.GetFileName(filePath);
        _viewModel.CurrentProfileId = profileId;

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
            $"[{progressPercent}%] Analyzing {fileName} with {profileId} ({remaining} remaining)",
            AnalysisLogLevel.Info);
        
        // Mark as finished if this is the very last work item
        if (_completedWorkItems >= _totalWorkItems && _currentFileNode != null)
        {
            _currentFileNode.Status = FileAnalysisStatus.Finished;
        }
    }

    public void ReportLlmRequest(string filePath, string profileId, string request)
    {
        // Store the request to pair with the response later
        var key = $"{filePath}|{profileId}";
        _pendingRequests[key] = (filePath, profileId, request);
        
        var fileName = Path.GetFileName(filePath);
        _viewModel.AddLog($"→ LLM Request for {fileName} ({profileId})", AnalysisLogLevel.Info);
    }

    public void ReportLlmResponse(string filePath, string profileId, string response)
    {
        var fileName = Path.GetFileName(filePath);
        var key = $"{filePath}|{profileId}";
        
        // Try to get the paired request
        string? request = null;
        if (_pendingRequests.TryGetValue(key, out var pendingRequest))
        {
            request = pendingRequest.Request;
            _pendingRequests.Remove(key);
        }
        
        // Create a detailed log entry with both request and response
        _viewModel.AddLog(
            $"← LLM Response for {fileName} ({profileId})",
            AnalysisLogLevel.Success,
            filePath,
            profileId,
            request,
            response);
    }

    public void ReportJsonParsingError(string filePath, string profileId, string error, string response)
    {
        var fileName = Path.GetFileName(filePath);
        var key = $"{filePath}|{profileId}";
        
        // Try to get the paired request
        string? request = null;
        if (_pendingRequests.TryGetValue(key, out var pendingRequest))
        {
            request = pendingRequest.Request;
            _pendingRequests.Remove(key);
        }
        
        _viewModel.AddLog(
            $"✗ JSON Parsing Error for {fileName} ({profileId}): {error}",
            AnalysisLogLevel.Error,
            filePath,
            profileId,
            request,
            response);
    }

    public void ReportIssues(string filePath, string profileId, IEnumerable<Issue> issues)
    {
        _viewModel.AddIssues(issues);
    }

    public void ReportBatchStart(int batchIndex, int totalBatches, IEnumerable<FileChange> files, string groupingReason)
    {
        // Clear previous batch highlights
        ClearBatchHighlights();
        
        // Update ViewModel batch properties
        _viewModel.CurrentBatchIndex = batchIndex;
        _viewModel.TotalBatches = totalBatches;
        _viewModel.CurrentBatchGroupingReason = groupingReason;
        _viewModel.CurrentBatchFiles.Clear();
        
        var fileList = files.ToList();
        foreach (var file in fileList)
        {
            _viewModel.CurrentBatchFiles.Add(Path.GetFileName(file.Path));
            
            // Mark file nodes as in current batch
            var node = _viewModel.FindFileNode(file.Path);
            if (node != null)
            {
                node.IsInCurrentBatch = true;
                node.Status = FileAnalysisStatus.Running;
            }
        }
        
        // Log the batch start with file list
        var fileNames = string.Join(", ", fileList.Select(f => Path.GetFileName(f.Path)));
        _viewModel.AddLog(
            $"📦 Batch {batchIndex}/{totalBatches}: {groupingReason} ({fileList.Count} files: {fileNames})",
            AnalysisLogLevel.Info);
    }

    private void ClearBatchHighlights()
    {
        // Recursively clear IsInCurrentBatch for all nodes
        void ClearNode(FileTreeNode node)
        {
            if (node.IsInCurrentBatch)
            {
                node.IsInCurrentBatch = false;
                // Mark completed batches as finished
                if (node.IsFile && node.Status == FileAnalysisStatus.Running)
                {
                    node.Status = FileAnalysisStatus.Finished;
                }
            }
            foreach (var child in node.Children)
            {
                ClearNode(child);
            }
        }
        
        if (_fileTree != null)
        {
            foreach (var node in _fileTree)
            {
                ClearNode(node);
            }
        }
    }
}
