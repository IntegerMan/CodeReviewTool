namespace MattEland.CodeReview.Core.Models;

/// <summary>
/// Represents the severity level of a code review issue.
/// </summary>
public enum Severity
{
    /// <summary>
    /// Informational finding, no action required.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Potential issue that should be reviewed.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Issue that should be fixed before merging.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical issue that must be addressed immediately.
    /// </summary>
    Critical = 3
}
