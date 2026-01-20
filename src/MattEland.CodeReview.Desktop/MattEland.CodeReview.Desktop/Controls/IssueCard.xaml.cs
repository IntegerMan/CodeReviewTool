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
        return severity switch
        {
            Severity.Critical => (
                Color.FromArgb(255, 196, 43, 28),   // #C42B1C
                Color.FromArgb(255, 253, 231, 233), // #FDE7E9
                "Critical"),
            Severity.Error => (
                Color.FromArgb(255, 157, 93, 0),    // #9D5D00
                Color.FromArgb(255, 255, 244, 206), // #FFF4CE
                "Error"),
            Severity.Warning => (
                Color.FromArgb(255, 15, 123, 15),   // #0F7B0F
                Color.FromArgb(255, 223, 246, 221), // #DFF6DD
                "Warning"),
            _ => (
                Color.FromArgb(255, 97, 97, 97),    // #616161
                Color.FromArgb(255, 240, 240, 240), // #F0F0F0
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
