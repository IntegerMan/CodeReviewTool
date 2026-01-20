using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MattEland.CodeReview.Core.Models;

using System.ComponentModel;

namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// Represents a node in the file tree (either a file or a folder).
/// </summary>
[Bindable(true)]
public partial class FileTreeNode : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private bool _isFile;

    [ObservableProperty]
    private FileAnalysisStatus _status = FileAnalysisStatus.Pending;

    [ObservableProperty]
    private int _issueCount;

    [ObservableProperty]
    private FileChangeType _changeType;

    [ObservableProperty]
    private int _linesAdded;

    [ObservableProperty]
    private int _linesDeleted;

    public ObservableCollection<FileTreeNode> Children { get; } = new();

    public FileTreeNode? Parent { get; set; }
}

/// <summary>
/// Represents the analysis status of a file.
/// </summary>
public enum FileAnalysisStatus
{
    Pending,
    Running,
    Finished
}
