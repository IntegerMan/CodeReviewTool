using CommunityToolkit.Mvvm.ComponentModel;
using MattEland.CodeReview.Core.Models;
using System.ComponentModel;

namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// ViewModel wrapper for a FileChange with selection support for the wizard.
/// </summary>
[Bindable(true)]
public partial class SelectableFileViewModel : ObservableObject
{
    public SelectableFileViewModel(FileChange fileChange)
    {
        FileChange = fileChange;
        IsSelected = true; // Selected by default
    }

    /// <summary>
    /// The underlying file change data.
    /// </summary>
    public FileChange FileChange { get; }

    /// <summary>
    /// Whether this file is selected for analysis.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// File path.
    /// </summary>
    public string Path => FileChange.Path;

    /// <summary>
    /// File name only.
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(FileChange.Path);

    /// <summary>
    /// Directory path.
    /// </summary>
    public string Directory => System.IO.Path.GetDirectoryName(FileChange.Path) ?? "";

    /// <summary>
    /// Change type (Added, Modified, Deleted, etc).
    /// </summary>
    public FileChangeType ChangeType => FileChange.ChangeType;

    /// <summary>
    /// Lines added.
    /// </summary>
    public int LinesAdded => FileChange.LinesAdded;

    /// <summary>
    /// Lines deleted.
    /// </summary>
    public int LinesDeleted => FileChange.LinesDeleted;

    /// <summary>
    /// Detected programming language.
    /// </summary>
    public string? Language => FileChange.Language;
}
