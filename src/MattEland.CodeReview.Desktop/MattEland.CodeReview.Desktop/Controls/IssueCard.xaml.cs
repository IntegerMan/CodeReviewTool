using Windows.UI;

namespace MattEland.CodeReview.Desktop.Controls;

public sealed partial class IssueCard : UserControl
{
    public static readonly DependencyProperty IssueProperty =
        DependencyProperty.Register(
            nameof(Issue),
            typeof(Issue),
            typeof(IssueCard),
            new PropertyMetadata(null, OnIssueChanged));

    public Issue Issue
    {
        get => (Issue)GetValue(IssueProperty);
        set => SetValue(IssueProperty, value);
    }

    public IssueCard()
    {
        this.InitializeComponent();
    }

    private static void OnIssueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IssueCard card && e.NewValue is Issue issue)
        {
            card.UpdateSeverityColors(issue.Severity);
        }
    }

    private void UpdateSeverityColors(Severity severity)
    {
        var (foreground, background, text) = GetSeverityColors(severity);
        
        SeverityBrush.Color = foreground;
        SeverityBackgroundBrush.Color = background;
        SeverityTextBrush.Color = foreground;
        SeverityText.Text = GetSeverityText(severity);
    }

    private static (Color foreground, Color background, string text) GetSeverityColors(Severity severity)
    {
        // Dark mode optimized colors
        return severity switch
        {
            Severity.Critical => (
                Color.FromArgb(255, 255, 107, 107),  // #FF6B6B - bright red foreground
                Color.FromArgb(255, 68, 39, 38),    // #442726 - dark red background
                "Critical"),
            Severity.Error => (
                Color.FromArgb(255, 255, 179, 102), // #FFB366 - bright amber foreground
                Color.FromArgb(255, 74, 60, 26),   // #4A3C1A - dark amber background
                "Error"),
            Severity.Warning => (
                Color.FromArgb(255, 107, 203, 119), // #6BCB77 - bright green foreground
                Color.FromArgb(255, 29, 61, 29),   // #1D3D1D - dark green background
                "Warning"),
            _ => (
                Color.FromArgb(255, 176, 176, 176), // #B0B0B0 - gray foreground
                Color.FromArgb(255, 58, 58, 58),   // #3A3A3A - dark gray background
                "Info")
        };
    }

    private static string GetSeverityText(Severity severity) => severity switch
    {
        Severity.Critical => "Critical",
        Severity.Error => "Error",
        Severity.Warning => "Warning",
        _ => "Info"
    };

    /// <summary>
    /// Returns Visible if the nullable int has a value.
    /// </summary>
    public static Visibility HasLine(int? line) => 
        line.HasValue ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns Visible if the string is not null or empty.
    /// </summary>
    public static Visibility HasText(string? text) => 
        !string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns Visible if confidence has a value.
    /// </summary>
    public static Visibility HasConfidence(double? confidence) => 
        confidence.HasValue ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Formats confidence as a percentage.
    /// </summary>
    public static string FormatConfidence(double? confidence) => 
        confidence.HasValue ? $"{confidence.Value:P0}" : string.Empty;
}
