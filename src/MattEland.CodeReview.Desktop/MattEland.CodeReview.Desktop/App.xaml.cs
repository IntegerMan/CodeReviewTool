using MattEland.CodeReview.Desktop.Services;
using MattEland.CodeReview.Desktop.ViewModels;
using Microsoft.Extensions.Hosting;
using Uno.Resizetizer;

namespace MattEland.CodeReview.Desktop;

public partial class App : Application
{
    private const string AppFolderName = "MattEland.CodeReview";
    private const string SettingsFileName = "settings.json";

    /// <summary>
    /// Gets the service provider for the application.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Gets the main window.
    /// </summary>
    public static Window? MainWindow { get; private set; }

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Build host with DI
        var host = CreateHostBuilder().Build();
        Services = host.Services;

        // Initialize rule provider and load user settings
        await Services.InitializeCodeReviewAsync();
        
        // Load user settings from local storage
        var settingsService = Services.GetRequiredService<IUserSettingsService>();
        await settingsService.LoadAsync();

        MainWindow = new Window();

        // Set preferred window size for better real estate
        MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1400, Height = 900 });

#if DEBUG
        MainWindow.UseStudio();
#endif

        // Do not repeat app initialization when the Window already has content
        if (MainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            MainWindow.Content = rootFrame;
            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        MainWindow.SetWindowIcon();
        MainWindow.Activate();
    }

    /// <summary>
    /// Creates the host builder with DI configuration.
    /// </summary>
    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                
                // Add user settings file as a configuration source (overrides appsettings.json)
                var userSettingsPath = GetUserSettingsFilePath();
                config.AddJsonFile(userSettingsPath, optional: true, reloadOnChange: true);
                
                config.AddEnvironmentVariables("CODEREVIEW_");
            })
            .ConfigureServices((context, services) =>
            {
                // Add code review core services
                services.AddCodeReview(context.Configuration);

                // Add user settings service
                services.AddSingleton<IUserSettingsService, UserSettingsService>();

                // Add ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<HomeViewModel>();
                services.AddSingleton<AnalysisViewModel>();
                services.AddSingleton<WizardViewModel>();
                services.AddTransient<RulesViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Add navigation service
                services.AddSingleton<INavigationService, NavigationService>();

                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });
    }

    /// <summary>
    /// Gets the path to the user settings file.
    /// </summary>
    private static string GetUserSettingsFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, AppFolderName, SettingsFileName);
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails.
    /// </summary>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    /// <summary>
    /// Configures global Uno Platform logging.
    /// </summary>
    public static void InitializeLogging()
    {
#if DEBUG
        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__ || __MACCATALYST__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#else
            builder.AddConsole();
#endif

            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
