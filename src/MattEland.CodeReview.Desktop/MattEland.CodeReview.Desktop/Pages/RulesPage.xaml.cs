using MattEland.CodeReview.Desktop.ViewModels;
using Windows.UI;

namespace MattEland.CodeReview.Desktop.Pages;

public sealed partial class RulesPage : Page
{
    public RulesViewModel ViewModel { get; }

    public RulesPage()
    {
        ViewModel = App.Services.GetRequiredService<RulesViewModel>();
        this.InitializeComponent();
    }

    /// <summary>
    /// Gets the color for a severity level.
    /// </summary>
    public static Color GetSeverityColor(Severity severity) => severity switch
    {
        Severity.Critical => Color.FromArgb(255, 196, 43, 28),  // #C42B1C
        Severity.Error => Color.FromArgb(255, 157, 93, 0),     // #9D5D00
        Severity.Warning => Color.FromArgb(255, 15, 123, 15),  // #0F7B0F
        _ => Color.FromArgb(255, 97, 97, 97)                    // #616161
    };

    /// <summary>
    /// Gets the background color for a severity level.
    /// </summary>
    public static Color GetSeverityBackgroundColor(Severity severity) => severity switch
    {
        Severity.Critical => Color.FromArgb(255, 253, 231, 233),  // #FDE7E9
        Severity.Error => Color.FromArgb(255, 255, 244, 206),     // #FFF4CE
        Severity.Warning => Color.FromArgb(255, 223, 246, 221),   // #DFF6DD
        _ => Color.FromArgb(255, 240, 240, 240)                    // #F0F0F0
    };

    /// <summary>
    /// Converts boolean to Visibility.
    /// </summary>
    public static Visibility ToVisibility(bool value) => 
        value ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns Visible if list has any items.
    /// </summary>
    public static Visibility VisibleIfAny<T>(IReadOnlyList<T> list) => 
        list.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns Visible if count is zero.
    /// </summary>
    public static Visibility IsEmpty(int count) => 
        count == 0 ? Visibility.Visible : Visibility.Collapsed;
}
