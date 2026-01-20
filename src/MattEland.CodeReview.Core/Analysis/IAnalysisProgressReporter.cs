namespace MattEland.CodeReview.Core.Analysis;

/// <summary>
/// Interface for reporting analysis progress and details.
/// </summary>
public interface IAnalysisProgressReporter
{
    /// <summary>
    /// Reports progress on file/rule processing.
    /// </summary>
    /// <param name="currentFile">Current file index (1-based).</param>
    /// <param name="totalFiles">Total number of files.</param>
    /// <param name="currentRule">Current rule index (1-based).</param>
    /// <param name="totalRules">Total number of rules.</param>
    /// <param name="filePath">Path of the file being analyzed.</param>
    /// <param name="ruleId">ID of the rule being applied.</param>
    void ReportProgress(int currentFile, int totalFiles, int currentRule, int totalRules, string filePath, string ruleId);

    /// <summary>
    /// Reports an LLM request being made.
    /// </summary>
    /// <param name="filePath">Path of the file being analyzed.</param>
    /// <param name="ruleId">ID of the rule being applied.</param>
    /// <param name="request">The request being sent to the LLM (truncated if too long).</param>
    void ReportLlmRequest(string filePath, string ruleId, string request);

    /// <summary>
    /// Reports an LLM response received.
    /// </summary>
    /// <param name="filePath">Path of the file being analyzed.</param>
    /// <param name="ruleId">ID of the rule being applied.</param>
    /// <param name="response">The response received from the LLM (truncated if too long).</param>
    void ReportLlmResponse(string filePath, string ruleId, string response);

    /// <summary>
    /// Reports a JSON parsing error.
    /// </summary>
    /// <param name="filePath">Path of the file being analyzed.</param>
    /// <param name="ruleId">ID of the rule being applied.</param>
    /// <param name="error">The parsing error message.</param>
    /// <param name="response">The response that failed to parse (truncated if too long).</param>
    void ReportJsonParsingError(string filePath, string ruleId, string error, string response);
}
