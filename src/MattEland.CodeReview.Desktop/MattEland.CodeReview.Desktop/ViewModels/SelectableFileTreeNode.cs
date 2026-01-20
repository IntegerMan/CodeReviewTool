using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// Represents a selectable node in the file selection wizard tree.
/// </summary>
[Bindable(true)]
public partial class SelectableFileTreeNode : ObservableObject
{
    private bool _isInternalSelectionChange;

    public SelectableFileTreeNode(string name, string fullPath, bool isFile, SelectableFileViewModel? fileViewModel = null)
    {
        Name = name;
        FullPath = fullPath;
        IsFile = isFile;
        FileViewModel = fileViewModel;

        if (fileViewModel != null)
        {
            _isSelected = fileViewModel.IsSelected;
            fileViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectableFileViewModel.IsSelected) && !_isInternalSelectionChange)
                {
                    IsSelected = fileViewModel.IsSelected;
                }
            };
        }
    }

    /// <summary>
    /// Node name.
    /// </summary>
    [ObservableProperty]
    private string _name;

    /// <summary>
    /// Relative path.
    /// </summary>
    [ObservableProperty]
    private string _fullPath;

    /// <summary>
    /// Whether this is a file or a folder.
    /// </summary>
    [ObservableProperty]
    private bool _isFile;

    /// <summary>
    /// The associated file ViewModel (for leaf nodes).
    /// </summary>
    public SelectableFileViewModel? FileViewModel { get; }

    /// <summary>
    /// Whether this node is selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected = true;

    /// <summary>
    /// Children nodes.
    /// </summary>
    public ObservableCollection<SelectableFileTreeNode> Children { get; } = [];

    /// <summary>
    /// Parent node.
    /// </summary>
    public SelectableFileTreeNode? Parent { get; set; }

    partial void OnIsSelectedChanged(bool value)
    {
        if (_isInternalSelectionChange) return;

        _isInternalSelectionChange = true;
        try
        {
            // Propagate down
            foreach (var child in Children)
            {
                child.IsSelected = value;
            }

            // Sync with file ViewModel if present
            if (FileViewModel != null)
            {
                FileViewModel.IsSelected = value;
            }

            // Propagate up (simplified: if any child selected, parent selected? No, usually if all children deselected, parent deselected.
            // For now, let's just make it simple: parent reflects children if we change it manually)
            Parent?.UpdateSelectionFromChildren();
        }
        finally
        {
            _isInternalSelectionChange = false;
        }
    }

    public void UpdateSelectionFromChildren()
    {
        if (IsFile || Children.Count == 0 || _isInternalSelectionChange) return;

        _isInternalSelectionChange = true;
        try
        {
            // If any child is selected, we consider the folder selected (or at least partially, but WinUI CheckBox usually just has bool)
            // Let's go with: if any child selected, folder is selected.
            var anySelected = Children.Any(c => c.IsSelected);
            if (IsSelected != anySelected)
            {
                IsSelected = anySelected;
                Parent?.UpdateSelectionFromChildren();
            }
        }
        finally
        {
            _isInternalSelectionChange = false;
        }
    }
}
