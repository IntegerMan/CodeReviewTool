using MattEland.CodeReview.Desktop.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MattEland.CodeReview.Desktop.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        this.DataContext = ViewModel;
        this.InitializeComponent();
    }

    /// <summary>
    /// Returns Visible if count is zero.
    /// </summary>
    public static Visibility IsEmpty(int count) => 
        count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets the InfoBarSeverity based on success/failure.
    /// </summary>
    public static InfoBarSeverity GetSeverity(bool isSuccess) =>
        isSuccess ? InfoBarSeverity.Success : InfoBarSeverity.Error;
}
