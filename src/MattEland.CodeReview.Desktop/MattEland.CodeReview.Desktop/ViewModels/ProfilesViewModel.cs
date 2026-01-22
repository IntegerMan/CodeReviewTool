namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Profiles page with profile management.
/// </summary>
public partial class ProfilesViewModel : ObservableObject
{
    private readonly IProfileProvider _profileProvider;
    private List<ReviewProfile> _allProfiles = [];

    public ProfilesViewModel(IProfileProvider profileProvider)
    {
        _profileProvider = profileProvider;
        LoadProfiles();
    }

    /// <summary>
    /// All available profiles.
    /// </summary>
    public ObservableCollection<ProfileViewModel> Profiles { get; } = [];

    /// <summary>
    /// Search text filter.
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Total number of profiles.
    /// </summary>
    public int TotalProfileCount => _allProfiles.Count;

    /// <summary>
    /// Number of profiles matching current filters.
    /// </summary>
    public int FilteredProfileCount => Profiles.Count;

    /// <summary>
    /// Number of enabled profiles.
    /// </summary>
    public int EnabledProfileCount => _allProfiles.Count(p => p.Enabled);

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    /// <summary>
    /// Loads all profiles from the provider.
    /// </summary>
    [RelayCommand]
    private void LoadProfiles()
    {
        _allProfiles = [.. _profileProvider.GetAllProfiles()];
        ApplyFilters();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
    }

    private void ApplyFilters()
    {
        Profiles.Clear();

        var filtered = _allProfiles.AsEnumerable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            filtered = filtered.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var profile in filtered.OrderBy(p => p.Name))
        {
            Profiles.Add(new ProfileViewModel(profile));
        }

        OnPropertyChanged(nameof(FilteredProfileCount));
    }
}

/// <summary>
/// ViewModel wrapper for a ReviewProfile with enable/disable support.
/// </summary>
public partial class ProfileViewModel : ObservableObject
{
    private readonly ReviewProfile _profile;

    public ProfileViewModel(ReviewProfile profile)
    {
        _profile = profile;
        _isEnabled = profile.Enabled;
    }

    public string Id => _profile.Id;
    public string Name => _profile.Name;
    public string Description => _profile.Description;

    [ObservableProperty]
    private bool _isEnabled;
}
