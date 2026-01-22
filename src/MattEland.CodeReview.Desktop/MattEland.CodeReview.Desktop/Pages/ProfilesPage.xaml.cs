using MattEland.CodeReview.Desktop.ViewModels;

namespace MattEland.CodeReview.Desktop.Pages;

public sealed partial class ProfilesPage : Page
{
    public ProfilesViewModel ViewModel { get; }

    public ProfilesPage()
    {
        ViewModel = App.Services.GetRequiredService<ProfilesViewModel>();
        this.InitializeComponent();
    }
}
