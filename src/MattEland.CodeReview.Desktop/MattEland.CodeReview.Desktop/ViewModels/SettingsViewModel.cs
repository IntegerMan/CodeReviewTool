using System.Net.Http;
using System.Text.Json;
using MattEland.CodeReview.Desktop.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace MattEland.CodeReview.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Settings page with provider and git configuration.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IOptions<CodeReviewOptions> _options;
    private readonly IUserSettingsService _settingsService;
    private CodeReviewOptions _originalOptions;

    public SettingsViewModel(IOptions<CodeReviewOptions> options, IUserSettingsService settingsService)
    {
        _options = options;
        _settingsService = settingsService;
        _originalOptions = options.Value;
        LoadFromOptions();
    }

    /// <summary>
    /// Available AI providers.
    /// </summary>
    public List<string> AvailableProviders { get; } = ["AzureOpenAI", "OpenAI", "Ollama"];

    /// <summary>
    /// Selected AI provider.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAzureOpenAI))]
    [NotifyPropertyChangedFor(nameof(IsOpenAI))]
    [NotifyPropertyChangedFor(nameof(IsOllama))]
    private string _selectedProvider = "Ollama";

    /// <summary>
    /// Whether Azure OpenAI is selected.
    /// </summary>
    public bool IsAzureOpenAI => SelectedProvider == "AzureOpenAI";

    /// <summary>
    /// Whether OpenAI is selected.
    /// </summary>
    public bool IsOpenAI => SelectedProvider == "OpenAI";

    /// <summary>
    /// Whether Ollama is selected.
    /// </summary>
    public bool IsOllama => SelectedProvider == "Ollama";

    // Azure OpenAI settings
    [ObservableProperty]
    private string _azureEndpoint = string.Empty;

    [ObservableProperty]
    private string _azureDeploymentName = "gpt-4o";

    [ObservableProperty]
    private string _azureApiKey = string.Empty;

    // OpenAI settings
    [ObservableProperty]
    private string _openAIApiKey = string.Empty;

    [ObservableProperty]
    private string _openAIModel = "gpt-4o";

    [ObservableProperty]
    private string _openAIOrganizationId = string.Empty;

    // Ollama settings
    [ObservableProperty]
    private string _ollamaEndpoint = "http://localhost:11434";

    [ObservableProperty]
    private string _ollamaModel = "llama3";

    // Git settings
    [ObservableProperty]
    private string _defaultBaseBranch = "main";

    [ObservableProperty]
    private int _maxFilesPerReview = 50;

    [ObservableProperty]
    private int _maxDiffSizePerFile = 10000;

    // Custom prompt paths
    public ObservableCollection<string> CustomPromptPaths { get; } = [];

    [ObservableProperty]
    private string _newPromptPath = string.Empty;

    /// <summary>
    /// Whether there are unsaved changes.
    /// </summary>
    [ObservableProperty]
    private bool _hasChanges;

    /// <summary>
    /// Status message for save operations.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Whether to show the toast notification.
    /// </summary>
    [ObservableProperty]
    private bool _showToast;

    /// <summary>
    /// Toast title.
    /// </summary>
    [ObservableProperty]
    private string _toastTitle = string.Empty;

    /// <summary>
    /// Toast message.
    /// </summary>
    [ObservableProperty]
    private string _toastMessage = string.Empty;

    /// <summary>
    /// Whether the toast is a success (true) or error (false).
    /// </summary>
    [ObservableProperty]
    private bool _toastIsSuccess;

    /// <summary>
    /// Adds a new custom prompt path.
    /// </summary>
    [RelayCommand]
    private void AddPromptPath()
    {
        if (!string.IsNullOrWhiteSpace(NewPromptPath) && !CustomPromptPaths.Contains(NewPromptPath))
        {
            CustomPromptPaths.Add(NewPromptPath);
            NewPromptPath = string.Empty;
            HasChanges = true;
        }
    }

    /// <summary>
    /// Removes a custom prompt path.
    /// </summary>
    [RelayCommand]
    private void RemovePromptPath(string path)
    {
        if (CustomPromptPaths.Remove(path))
        {
            HasChanges = true;
        }
    }

    /// <summary>
    /// Saves the current settings.
    /// </summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            // Update the in-memory options object
            var options = _options.Value;
            
            options.Provider = SelectedProvider;
            
            options.AzureOpenAI.Endpoint = AzureEndpoint;
            options.AzureOpenAI.DeploymentName = AzureDeploymentName;
            options.AzureOpenAI.ApiKey = AzureApiKey;
            
            options.OpenAI.ApiKey = OpenAIApiKey;
            options.OpenAI.Model = OpenAIModel;
            options.OpenAI.OrganizationId = string.IsNullOrWhiteSpace(OpenAIOrganizationId) ? null : OpenAIOrganizationId;
            
            options.Ollama.Endpoint = OllamaEndpoint;
            options.Ollama.Model = OllamaModel;
            
            options.Git.DefaultBaseBranch = DefaultBaseBranch;
            options.Git.MaxFilesPerReview = MaxFilesPerReview;
            options.Git.MaxDiffSizePerFile = MaxDiffSizePerFile;
            
            options.CustomPromptPaths.Clear();
            options.CustomPromptPaths.AddRange(CustomPromptPaths);

            // Persist to user settings file
            await PersistToUserSettingsAsync();

            ShowToastNotification(true, "Settings Saved", "Your settings have been saved successfully.");
            HasChanges = false;
        }
        catch (Exception ex)
        {
            ShowToastNotification(false, "Save Failed", $"Failed to save settings: {ex.Message}");
        }
    }

    private async Task PersistToUserSettingsAsync()
    {
        // Update the user settings with current values
        var settings = _settingsService.Settings;
        
        settings.CodeReview.Provider = SelectedProvider;
        
        settings.CodeReview.AzureOpenAI.Endpoint = AzureEndpoint;
        settings.CodeReview.AzureOpenAI.DeploymentName = AzureDeploymentName;
        settings.CodeReview.AzureOpenAI.ApiKey = AzureApiKey;
        
        settings.CodeReview.OpenAI.ApiKey = OpenAIApiKey;
        settings.CodeReview.OpenAI.Model = OpenAIModel;
        settings.CodeReview.OpenAI.OrganizationId = string.IsNullOrWhiteSpace(OpenAIOrganizationId) ? null : OpenAIOrganizationId;
        
        settings.CodeReview.Ollama.Endpoint = OllamaEndpoint;
        settings.CodeReview.Ollama.Model = OllamaModel;
        
        settings.CodeReview.Git.DefaultBaseBranch = DefaultBaseBranch;
        settings.CodeReview.Git.MaxFilesPerReview = MaxFilesPerReview;
        settings.CodeReview.Git.MaxDiffSizePerFile = MaxDiffSizePerFile;
        
        settings.CodeReview.CustomPromptPaths.Clear();
        settings.CodeReview.CustomPromptPaths.AddRange(CustomPromptPaths);

        // Save to file
        await _settingsService.SaveAsync();
    }

    /// <summary>
    /// Resets settings to their original values.
    /// </summary>
    [RelayCommand]
    private void ResetSettings()
    {
        LoadFromOptions();
        HasChanges = false;
        StatusMessage = "Settings reset to original values.";
    }

    /// <summary>
    /// Tests the connection to the selected provider.
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        ShowToast = false;
        StatusMessage = "Testing connection...";
        
        try
        {
            // Attempt to actually test the connection based on provider
            bool success = false;
            string message = string.Empty;

            switch (SelectedProvider)
            {
                case "Ollama":
                    // Test Ollama connection and verify model exists
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                        
                        // First, check if endpoint is reachable
                        var tagsResponse = await client.GetAsync($"{OllamaEndpoint}/api/tags");
                        if (!tagsResponse.IsSuccessStatusCode)
                        {
                            message = $"Failed to connect to Ollama: {tagsResponse.StatusCode}";
                            break;
                        }
                        
                        // Parse the response to get available models
                        var tagsContent = await tagsResponse.Content.ReadAsStringAsync();
                        var tagsJson = JsonSerializer.Deserialize<JsonElement>(tagsContent);
                        
                        if (tagsJson.TryGetProperty("models", out var modelsArray))
                        {
                            var availableModels = modelsArray.EnumerateArray()
                                .Select(m => m.TryGetProperty("name", out var name) ? name.GetString() : null)
                                .Where(n => n != null)
                                .ToList();
                            
                            if (availableModels.Contains(OllamaModel))
                            {
                                success = true;
                                message = $"Successfully connected to Ollama. Model '{OllamaModel}' is available.";
                            }
                            else
                            {
                                success = false;
                                var modelsList = string.Join(", ", availableModels.Take(5));
                                if (availableModels.Count > 5) modelsList += ", ...";
                                message = $"Model '{OllamaModel}' not found. Available models: {modelsList}";
                            }
                        }
                        else
                        {
                            // Fallback: try to make a simple test call with the model
                            try
                            {
                                var testRequest = new
                                {
                                    model = OllamaModel,
                                    prompt = "test",
                                    stream = false
                                };
                                
                                var jsonContent = JsonSerializer.Serialize(testRequest);
                                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                                var testResponse = await client.PostAsync($"{OllamaEndpoint}/api/generate", content);
                                
                                if (testResponse.IsSuccessStatusCode)
                                {
                                    success = true;
                                    message = $"Successfully connected to Ollama. Model '{OllamaModel}' is available.";
                                }
                                else
                                {
                                    var errorContent = await testResponse.Content.ReadAsStringAsync();
                                    success = false;
                                    message = $"Model '{OllamaModel}' test failed: {testResponse.StatusCode}. {errorContent}";
                                }
                            }
                            catch (Exception ex)
                            {
                                success = false;
                                message = $"Failed to test model '{OllamaModel}': {ex.Message}";
                            }
                        }
                    }
                    break;
                    
                case "AzureOpenAI":
                    success = !string.IsNullOrWhiteSpace(AzureEndpoint) && !string.IsNullOrWhiteSpace(AzureApiKey);
                    message = success 
                        ? "Azure OpenAI configuration looks valid" 
                        : "Missing Azure OpenAI endpoint or API key";
                    break;
                    
                case "OpenAI":
                    success = !string.IsNullOrWhiteSpace(OpenAIApiKey);
                    message = success 
                        ? "OpenAI configuration looks valid" 
                        : "Missing OpenAI API key";
                    break;
                    
                default:
                    message = "Unknown provider";
                    break;
            }

            ShowToastNotification(success, 
                success ? "Connection Successful" : "Connection Failed", 
                message);
        }
        catch (Exception ex)
        {
            ShowToastNotification(false, "Connection Failed", ex.Message);
        }
        
        StatusMessage = string.Empty;
    }

    private void ShowToastNotification(bool isSuccess, string title, string message)
    {
        ToastIsSuccess = isSuccess;
        ToastTitle = title;
        ToastMessage = message;
        ShowToast = true;
    }

    /// <summary>
    /// Closes the toast notification.
    /// </summary>
    [RelayCommand]
    private void CloseToast()
    {
        ShowToast = false;
    }

    private void LoadFromOptions()
    {
        var options = _options.Value;
        
        SelectedProvider = options.Provider;
        
        AzureEndpoint = options.AzureOpenAI.Endpoint ?? string.Empty;
        AzureDeploymentName = options.AzureOpenAI.DeploymentName;
        AzureApiKey = options.AzureOpenAI.ApiKey ?? string.Empty;
        
        OpenAIApiKey = options.OpenAI.ApiKey ?? string.Empty;
        OpenAIModel = options.OpenAI.Model;
        OpenAIOrganizationId = options.OpenAI.OrganizationId ?? string.Empty;
        
        OllamaEndpoint = options.Ollama.Endpoint;
        OllamaModel = options.Ollama.Model;
        
        DefaultBaseBranch = options.Git.DefaultBaseBranch;
        MaxFilesPerReview = options.Git.MaxFilesPerReview;
        MaxDiffSizePerFile = options.Git.MaxDiffSizePerFile;
        
        CustomPromptPaths.Clear();
        foreach (var path in options.CustomPromptPaths)
        {
            CustomPromptPaths.Add(path);
        }
    }
}
