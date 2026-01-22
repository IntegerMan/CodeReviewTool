using MattEland.CodeReview.Desktop.ViewModels;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MattEland.CodeReview.Core.Models;
using Windows.UI;

namespace MattEland.CodeReview.Desktop.Pages;

public sealed partial class AnalysisPage : Page
{
    public AnalysisViewModel ViewModel { get; }
    public WizardViewModel Wizard { get; }

    public AnalysisPage()
    {
        ViewModel = App.Services.GetRequiredService<AnalysisViewModel>();
        Wizard = App.Services.GetRequiredService<WizardViewModel>();
        this.DataContext = this; // Use the page itself as DataContext for binding to both
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.RefreshRepositoryState();
        Wizard.RefreshRepositoryState();
    }

    /// <summary>
    /// Returns true if the current step matches the parameter.
    /// </summary>
    public static bool IsStep(WizardStep current, int step) => (int)current == step;

    /// <summary>
    /// Navigates to the results tab after analysis.
    /// </summary>
    public void NavigateToResults()
    {
        ViewModel.SelectedTabIndex = 1;
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
    /// Returns Visible if current step does NOT match the parameter.
    /// </summary>
    public static Visibility VisibleIfNotStep(WizardStep current, int step) => 
        (int)current != step ? Visibility.Visible : Visibility.Collapsed;

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
    /// Gets the icon for a file system node.
    /// </summary>
    public static string GetNodeIcon(bool isFile) => isFile ? "\uE8A5" : "\uE8B7";

    /// <summary>
    /// Gets the status icon for a file.
    /// </summary>
    public static string GetStatusIcon(FileAnalysisStatus status) => status switch
    {
        FileAnalysisStatus.Running => "\uE72C",   // Sync icon
        FileAnalysisStatus.Finished => "\uE73E",  // Checkmark icon
        _ => "\uE823"                              // Clock icon (Pending)
    };

    /// <summary>
    /// Gets the color for a file status.
    /// </summary>
    public static Color GetStatusColor(FileAnalysisStatus status) => status switch
    {
        FileAnalysisStatus.Running => Color.FromArgb(255, 0, 120, 212),    // Blue
        FileAnalysisStatus.Finished => Color.FromArgb(255, 107, 203, 119), // Green (dark mode friendly)
        _ => Color.FromArgb(255, 128, 128, 128)                            // Gray
    };

    /// <summary>
    /// Gets the color for a log level.
    /// </summary>
    public static Color GetLogColor(AnalysisLogLevel level) => level switch
    {
        AnalysisLogLevel.Success => Color.FromArgb(255, 107, 203, 119),  // Green
        AnalysisLogLevel.Warning => Color.FromArgb(255, 255, 179, 102),  // Amber
        AnalysisLogLevel.Error => Color.FromArgb(255, 255, 107, 107),    // Red
        _ => Color.FromArgb(255, 176, 176, 176)                          // Gray
    };

    /// <summary>
    /// Gets the icon for a file change type.
    /// </summary>
    public static string GetChangeTypeIcon(FileChangeType changeType) => changeType switch
    {
        FileChangeType.Added => "\uE710",     // + Add icon
        FileChangeType.Deleted => "\uE738",   // - Delete icon
        FileChangeType.Renamed => "\uE8AC",   // Rename icon
        FileChangeType.Copied => "\uE8C8",    // Copy icon
        _ => "\uE70F"                         // Edit icon (Modified)
    };

    /// <summary>
    /// Gets the color for a file change type.
    /// </summary>
    public static Color GetChangeTypeColor(FileChangeType changeType) => changeType switch
    {
        FileChangeType.Added => Color.FromArgb(255, 107, 203, 119),    // Green
        FileChangeType.Deleted => Color.FromArgb(255, 255, 107, 107),  // Red
        FileChangeType.Renamed => Color.FromArgb(255, 138, 180, 248),  // Blue
        FileChangeType.Copied => Color.FromArgb(255, 138, 180, 248),   // Blue
        _ => Color.FromArgb(255, 255, 179, 102)                        // Amber (Modified)
    };

    /// <summary>
    /// Formats line changes as +N/-N string.
    /// </summary>
    public static string FormatLineChanges(int added, int deleted) => 
        $"+{added}/-{deleted}";

    /// <summary>
    /// Returns Visible if there are any line changes.
    /// </summary>
    public static Visibility HasLineChanges(int added, int deleted) => 
        (added > 0 || deleted > 0) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns true if the is a file node.
    /// </summary>
    public static Visibility VisibleIfFile(bool isFile) => 
        isFile ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets the background brush for a file in the current batch.
    /// </summary>
    public static Brush GetBatchHighlightBrush(bool isInCurrentBatch) =>
        isInCurrentBatch 
            ? new SolidColorBrush(Color.FromArgb(50, 100, 149, 237))  // Light blue highlight
            : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));        // Transparent


    /// <summary>
    /// Gets the opacity for a wizard step.
    /// </summary>
    public static double GetStepOpacity(WizardStep current, int step) => 
        (int)current == step ? 1.0 : 0.4;

    /// <summary>
    /// Gets the background for a wizard step bubble.
    /// </summary>
    public static Brush GetStepBackground(WizardStep current, int step)
    {
        if ((int)current == step) return (Brush)Application.Current.Resources["SystemAccentColor"];
        if ((int)current > step) return (Brush)Application.Current.Resources["SystemAccentColorLight1"];
        return (Brush)Application.Current.Resources["ControlFillColorSecondaryBrush"];
    }

    /// <summary>
    /// Returns Visible if current step matches.
    /// </summary>
    public static Visibility VisibleIfStep(WizardStep current, int step) => 
        (int)current == step ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns true if the next button should be visible.
    /// </summary>
    public static Visibility GetNextButtonVisibility(WizardStep current) => 
        current != WizardStep.Analysis ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Returns Visible if value is true.
    /// </summary>
    public static Visibility VisibleIfTrue(bool value) => 
        value ? Visibility.Visible : Visibility.Collapsed;

    private async void OnNextClicked(object sender, RoutedEventArgs e)
    {
        if (Wizard.CurrentStep == WizardStep.Profiles)
        {
            // The Next button on step 3 transition to Step 4 (Analysis)
            // But we need to actually TRIGGER the analysis on AnalysisViewModel
            var diff = Wizard.GetFilteredDiff();
            var profileIds = Wizard.GetSelectedProfileIds();
            
            // Advance wizard to step 4
            Wizard.CurrentStep = WizardStep.Analysis;

            // Trigger analysis
            await ViewModel.RunAnalysisAsync(diff, profileIds, default);
        }
        else if (Wizard.CurrentStep == WizardStep.Analysis)
        {
            // "Done" button - reset wizard
            Wizard.ResetCommand.Execute(null);
        }
        else
        {
            // Other steps - just move next
            await Wizard.NextCommand.ExecuteAsync(null);
        }
    }
}

