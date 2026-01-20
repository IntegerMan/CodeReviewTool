using MattEland.CodeReview.Desktop.ViewModels;

namespace MattEland.CodeReview.Desktop.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.Services.GetRequiredService<HomeViewModel>();
        this.DataContext = ViewModel;
        this.InitializeComponent();
    }

    /// <summary>
    /// Helper for visibility binding to check if collection is empty.
    /// </summary>
    public static Visibility IsEmpty(int count) => count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Helper for visibility binding to check if collection has items.
    /// </summary>
    public static Visibility HasItems(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;
}
