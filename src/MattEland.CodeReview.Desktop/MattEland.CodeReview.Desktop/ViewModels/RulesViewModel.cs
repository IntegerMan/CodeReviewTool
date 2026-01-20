namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Rules page with filtering and rule management.
/// </summary>
public partial class RulesViewModel : ObservableObject
{
    private readonly IRuleProvider _ruleProvider;
    private List<Rule> _allRules = [];

    public RulesViewModel(IRuleProvider ruleProvider)
    {
        _ruleProvider = ruleProvider;
        LoadRules();
    }

    /// <summary>
    /// All available rules.
    /// </summary>
    public ObservableCollection<RuleViewModel> Rules { get; } = [];

    /// <summary>
    /// Available languages for filtering.
    /// </summary>
    public ObservableCollection<string> Languages { get; } = ["All"];

    /// <summary>
    /// Available categories for filtering.
    /// </summary>
    public ObservableCollection<string> Categories { get; } = ["All"];

    /// <summary>
    /// Selected language filter.
    /// </summary>
    [ObservableProperty]
    private string _selectedLanguage = "All";

    /// <summary>
    /// Selected category filter.
    /// </summary>
    [ObservableProperty]
    private string _selectedCategory = "All";

    /// <summary>
    /// Search text filter.
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Total number of rules.
    /// </summary>
    public int TotalRuleCount => _allRules.Count;

    /// <summary>
    /// Number of rules matching current filters.
    /// </summary>
    public int FilteredRuleCount => Rules.Count;

    /// <summary>
    /// Number of enabled rules.
    /// </summary>
    public int EnabledRuleCount => _allRules.Count(r => r.Enabled);

    partial void OnSelectedLanguageChanged(string value)
    {
        UpdateCategories();
        ApplyFilters();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    /// <summary>
    /// Loads all rules from the provider.
    /// </summary>
    [RelayCommand]
    private void LoadRules()
    {
        _allRules = [.. _ruleProvider.GetAllRules()];
        
        // Update languages
        Languages.Clear();
        Languages.Add("All");
        foreach (var lang in _ruleProvider.GetLanguages().OrderBy(l => l))
        {
            Languages.Add(lang.ToUpperInvariant());
        }

        UpdateCategories();
        ApplyFilters();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        SelectedLanguage = "All";
        SelectedCategory = "All";
        SearchText = string.Empty;
    }

    private void UpdateCategories()
    {
        Categories.Clear();
        Categories.Add("All");

        IEnumerable<string> categories;
        if (string.IsNullOrEmpty(SelectedLanguage) || SelectedLanguage == "All")
        {
            categories = _allRules.Select(r => r.Category).Distinct();
        }
        else
        {
            categories = _ruleProvider.GetCategories(SelectedLanguage.ToLowerInvariant()) ?? [];
        }

        foreach (var cat in categories.Where(c => !string.IsNullOrEmpty(c)).OrderBy(c => c))
        {
            Categories.Add(char.ToUpper(cat[0]) + cat[1..]);
        }

        if (string.IsNullOrEmpty(SelectedCategory) || !Categories.Contains(SelectedCategory))
        {
            SelectedCategory = "All";
        }
    }

    private void ApplyFilters()
    {
        Rules.Clear();

        var filtered = _allRules.AsEnumerable();

        // Language filter
        if (SelectedLanguage != "All")
        {
            filtered = filtered.Where(r => 
                string.Equals(r.Language, SelectedLanguage, StringComparison.OrdinalIgnoreCase));
        }

        // Category filter
        if (SelectedCategory != "All")
        {
            filtered = filtered.Where(r => 
                string.Equals(r.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            filtered = filtered.Where(r =>
                r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Tags.Any(t => t.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var rule in filtered.OrderBy(r => r.Language).ThenBy(r => r.Category).ThenBy(r => r.Name))
        {
            Rules.Add(new RuleViewModel(rule));
        }

        OnPropertyChanged(nameof(FilteredRuleCount));
    }
}

/// <summary>
/// ViewModel wrapper for a Rule with enable/disable support.
/// </summary>
public partial class RuleViewModel : ObservableObject
{
    private readonly Rule _rule;

    public RuleViewModel(Rule rule)
    {
        _rule = rule;
        _isEnabled = rule.Enabled;
    }

    public string Id => _rule.Id;
    public string Name => _rule.Name;
    public string Description => _rule.Description;
    public string Language => _rule.Language.ToUpperInvariant();
    public string Category => char.ToUpper(_rule.Category[0]) + _rule.Category[1..];
    public Severity DefaultSeverity => _rule.DefaultSeverity;
    public IReadOnlyList<string> Tags => _rule.Tags;
    public bool IsEmbedded => _rule.IsEmbedded;

    [ObservableProperty]
    private bool _isEnabled;

    public string SeverityDisplay => DefaultSeverity switch
    {
        Severity.Critical => "Critical",
        Severity.Error => "Error",
        Severity.Warning => "Warning",
        _ => "Info"
    };
}
