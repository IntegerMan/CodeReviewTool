using MattEland.CodeReview.Desktop.Pages;

namespace MattEland.CodeReview.Desktop.Services;

/// <summary>
/// Service for navigation between pages in the application.
/// </summary>
public class NavigationService : INavigationService
{
    private Frame? _frame;

    /// <summary>
    /// Sets the navigation frame to use.
    /// </summary>
    public void SetFrame(Frame frame)
    {
        _frame = frame;
    }

    /// <summary>
    /// Navigates to the specified page type.
    /// </summary>
    public bool Navigate<TPage>() where TPage : Page
    {
        return _frame?.Navigate(typeof(TPage)) ?? false;
    }

    /// <summary>
    /// Navigates to the specified page type with a parameter.
    /// </summary>
    public bool Navigate<TPage>(object parameter) where TPage : Page
    {
        return _frame?.Navigate(typeof(TPage), parameter) ?? false;
    }

    /// <summary>
    /// Navigates to the Home page.
    /// </summary>
    public bool NavigateToHome() => Navigate<HomePage>();

    /// <summary>
    /// Navigates to the Analysis page.
    /// </summary>
    public bool NavigateToAnalysis() => Navigate<AnalysisPage>();

    /// <summary>
    /// Navigates to the Rules page.
    /// </summary>
    public bool NavigateToRules() => Navigate<RulesPage>();

    /// <summary>
    /// Navigates to the Settings page.
    /// </summary>
    public bool NavigateToSettings() => Navigate<SettingsPage>();

    /// <summary>
    /// Goes back in the navigation stack if possible.
    /// </summary>
    public bool GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Whether navigation can go back.
    /// </summary>
    public bool CanGoBack => _frame?.CanGoBack ?? false;
}

/// <summary>
/// Interface for navigation service.
/// </summary>
public interface INavigationService
{
    void SetFrame(Frame frame);
    bool Navigate<TPage>() where TPage : Page;
    bool Navigate<TPage>(object parameter) where TPage : Page;
    bool NavigateToHome();
    bool NavigateToAnalysis();
    bool NavigateToRules();
    bool NavigateToSettings();
    bool GoBack();
    bool CanGoBack { get; }
}
