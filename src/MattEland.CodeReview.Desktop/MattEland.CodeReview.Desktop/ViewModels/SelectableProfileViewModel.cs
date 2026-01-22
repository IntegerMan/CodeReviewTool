using System.ComponentModel;

namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// ViewModel wrapper for a ReviewProfile with selection support for the wizard.
/// </summary>
[Bindable(true)]
public partial class SelectableProfileViewModel : ObservableObject
{
    private readonly ReviewProfile _profile;

    public SelectableProfileViewModel(ReviewProfile profile)
    {
        _profile = profile;
        IsSelected = profile.Enabled; // Pre-select enabled profiles
    }

    /// <summary>
    /// The underlying profile data.
    /// </summary>
    public ReviewProfile Profile => _profile;

    /// <summary>
    /// Whether this profile is selected for this analysis run.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Profile ID.
    /// </summary>
    public string Id => _profile.Id;

    /// <summary>
    /// Profile display name.
    /// </summary>
    public string Name => _profile.Name;

    /// <summary>
    /// Profile description.
    /// </summary>
    public string Description => _profile.Description;

    /// <summary>
    /// Whether this profile is globally enabled.
    /// </summary>
    public bool IsEnabled => _profile.Enabled;
}
