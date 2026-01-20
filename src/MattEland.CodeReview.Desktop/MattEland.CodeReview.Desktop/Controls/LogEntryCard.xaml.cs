using MattEland.CodeReview.Desktop.ViewModels;
using Windows.UI;

namespace MattEland.CodeReview.Desktop.Controls;

public sealed partial class LogEntryCard : UserControl
{
    public static readonly DependencyProperty LogEntryProperty =
        DependencyProperty.Register(
            nameof(LogEntry),
            typeof(AnalysisLogEntry),
            typeof(LogEntryCard),
            new PropertyMetadata(null));

    public AnalysisLogEntry LogEntry
    {
        get => (AnalysisLogEntry)GetValue(LogEntryProperty);
        set => SetValue(LogEntryProperty, value);
    }

    public LogEntryCard()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Gets the icon glyph for a log level.
    /// </summary>
    public static string GetLevelIcon(AnalysisLogLevel level) => level switch
    {
        AnalysisLogLevel.Success => "\uE73E",   // Checkmark
        AnalysisLogLevel.Warning => "\uE7BA",   // Warning
        AnalysisLogLevel.Error => "\uEA39",     // Error X
        _ => "\uE946"                           // Info
    };

    /// <summary>
    /// Gets the color for a log level.
    /// </summary>
    public static Color GetLevelColor(AnalysisLogLevel level) => level switch
    {
        AnalysisLogLevel.Success => Color.FromArgb(255, 107, 203, 119),  // Green
        AnalysisLogLevel.Warning => Color.FromArgb(255, 255, 179, 102),  // Amber
        AnalysisLogLevel.Error => Color.FromArgb(255, 255, 107, 107),    // Red
        _ => Color.FromArgb(255, 176, 176, 176)                          // Gray
    };

    /// <summary>
    /// Formats a DateTime as compact time string.
    /// </summary>
    public static string FormatTime(DateTime time) => time.ToString("HH:mm:ss");

    /// <summary>
    /// Returns Visible if value is true.
    /// </summary>
    public static Visibility VisibleIf(bool value) => 
        value ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns Visible if value is false.
    /// </summary>
    public static Visibility Not(bool value) => 
        value ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Returns Visible if string has content.
    /// </summary>
    public static Visibility HasText(string? text) => 
        !string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;
}
