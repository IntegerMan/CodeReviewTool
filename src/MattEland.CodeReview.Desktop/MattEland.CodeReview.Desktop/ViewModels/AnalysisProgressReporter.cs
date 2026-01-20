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

    public AnalysisProgressReporter(AnalysisViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void SetTotalWorkItems(int total)
    {
        _totalWorkItems = total;
        _completedWorkItems = 0;
    }

    public void ReportProgress(int currentFile, int totalFiles, int currentRule, int totalRules, string filePath, string ruleId)
    {
        _completedWorkItems++;
        var progressPercent = _totalWorkItems > 0 
            ? (int)((_completedWorkItems / (double)_totalWorkItems) * 100) 
            : 0;
        
        var fileName = Path.GetFileName(filePath);
        var remaining = _totalWorkItems - _completedWorkItems;
        _viewModel.AddLog(
            $"[{progressPercent}%] ({_completedWorkItems}/{_totalWorkItems} complete, {remaining} remaining) Analyzing {fileName} with rule {ruleId}",
            AnalysisLogLevel.Info);
    }

    public void ReportLlmRequest(string filePath, string ruleId, string request)
    {
        var fileName = Path.GetFileName(filePath);
        _viewModel.AddLog($"→ LLM Request for {fileName} ({ruleId}):", AnalysisLogLevel.Info);
        _viewModel.AddLog($"  {request}", AnalysisLogLevel.Info);
    }

    public void ReportLlmResponse(string filePath, string ruleId, string response)
    {
        var fileName = Path.GetFileName(filePath);
        _viewModel.AddLog($"← LLM Response for {fileName} ({ruleId}):", AnalysisLogLevel.Info);
        _viewModel.AddLog($"  {response}", AnalysisLogLevel.Info);
    }

    public void ReportJsonParsingError(string filePath, string ruleId, string error, string response)
    {
        var fileName = Path.GetFileName(filePath);
        _viewModel.AddLog($"✗ JSON Parsing Error for {fileName} ({ruleId}):", AnalysisLogLevel.Error);
        _viewModel.AddLog($"  Error: {error}", AnalysisLogLevel.Error);
        _viewModel.AddLog($"  Response: {response}", AnalysisLogLevel.Error);
    }
}
