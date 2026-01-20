using MattEland.CodeReview.Desktop.ViewModels;
using Windows.UI;

namespace MattEland.CodeReview.Desktop.Pages;

public sealed partial class AnalysisPage : Page
{
    public AnalysisViewModel ViewModel { get; }

    public AnalysisPage()
    {
        ViewModel = App.Services.GetRequiredService<AnalysisViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.RefreshRepositoryState();
    }

    /// <summary>
    /// Helper for visibility binding - returns Visible if count > 0.
    /// </summary>
    public static Visibility VisibleIfPositive(int count) => 
        count > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Helper for visibility binding - returns Visible if false.
    /// </summary>
    public static Visibility Not(bool value) => 
        value ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Helper for bool binding - returns true if value is false.
    /// </summary>
    public static bool IsNotTrue(bool value) => !value;

    /// <summary>
    /// Returns Visible if count is zero.
    /// </summary>
    public static Visibility IsEmpty(int count) => 
        count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns Visible if count is greater than zero.
    /// </summary>
    public static Visibility HasItems(int count) => 
        count > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Formats a DateTime as time string.
    /// </summary>
    public static string FormatTime(DateTime time) => time.ToString("HH:mm:ss");

    /// <summary>
    /// Gets the color for a log level.
    /// </summary>
    public static Color GetLogColor(AnalysisLogLevel level) => level switch
    {
        AnalysisLogLevel.Success => Color.FromArgb(255, 15, 123, 15),   // Green
        AnalysisLogLevel.Warning => Color.FromArgb(255, 157, 93, 0),   // Amber
        AnalysisLogLevel.Error => Color.FromArgb(255, 196, 43, 28),    // Red
        _ => Color.FromArgb(255, 97, 97, 97)                           // Gray
    };
}
