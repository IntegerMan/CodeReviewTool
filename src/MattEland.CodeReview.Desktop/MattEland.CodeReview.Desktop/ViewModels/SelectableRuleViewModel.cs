namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// ViewModel wrapper for a Rule with selection support for the wizard.
/// </summary>
public partial class SelectableRuleViewModel : ObservableObject
{
    private readonly Rule _rule;

    public SelectableRuleViewModel(Rule rule)
    {
        _rule = rule;
        IsSelected = rule.Enabled; // Pre-select enabled rules
    }

    /// <summary>
    /// The underlying rule data.
    /// </summary>
    public Rule Rule => _rule;

    /// <summary>
    /// Whether this rule is selected for this analysis run.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Rule ID.
    /// </summary>
    public string Id => _rule.Id;

    /// <summary>
    /// Rule display name.
    /// </summary>
    public string Name => _rule.Name;

    /// <summary>
    /// Rule description.
    /// </summary>
    public string Description => _rule.Description;

    /// <summary>
    /// Language this rule applies to (uppercase).
    /// </summary>
    public string Language => _rule.Language.ToUpperInvariant();

    /// <summary>
    /// Category (title case).
    /// </summary>
    public string Category => char.ToUpper(_rule.Category[0]) + _rule.Category[1..];

    /// <summary>
    /// Default severity.
    /// </summary>
    public Severity DefaultSeverity => _rule.DefaultSeverity;

    /// <summary>
    /// Whether this rule is globally enabled.
    /// </summary>
    public bool IsEnabled => _rule.Enabled;

    /// <summary>
    /// Whether this is a built-in rule.
    /// </summary>
    public bool IsEmbedded => _rule.IsEmbedded;

    /// <summary>
    /// Tags for the rule.
    /// </summary>
    public IReadOnlyList<string> Tags => _rule.Tags;

    /// <summary>
    /// Display string for severity.
    /// </summary>
    public string SeverityDisplay => DefaultSeverity switch
    {
        Severity.Critical => "Critical",
        Severity.Error => "Error",
        Severity.Warning => "Warning",
        _ => "Info"
    };
}
