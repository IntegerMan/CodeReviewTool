using MattEland.CodeReview.Desktop.Pages;
using MattEland.CodeReview.Desktop.ViewModels;

namespace MattEland.CodeReview.Desktop;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        this.InitializeComponent();
        
        // Subscribe to navigation requests
        ViewModel.NavigationRequested += OnNavigationRequested;
    }

    private void OnNavigationRequested(object? sender, string pageTag)
    {
        // Find the navigation item with the matching tag and select it
        foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            if (item.Tag?.ToString() == pageTag)
            {
                NavView.SelectedItem = item;
                return;
            }
        }
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Select the first item (Home) by default
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItemContainer is NavigationViewItem item)
        {
            NavigateToPage(item.Tag?.ToString());
        }
    }

    private void NavigateToPage(string? tag)
    {
        Type? pageType = tag switch
        {
            "HomePage" => typeof(HomePage),
            "AnalysisPage" => typeof(AnalysisPage),
            "ProfilesPage" => typeof(ProfilesPage),
            _ => null
        };

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load page {e.SourcePageType.FullName}: {e.Exception}");
    }
}
