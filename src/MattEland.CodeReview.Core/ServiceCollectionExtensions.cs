using MattEland.CodeReview.Core.Analysis;
using MattEland.CodeReview.Core.Configuration;
using MattEland.CodeReview.Core.Git;
using MattEland.CodeReview.Core.Prompts;
using MattEland.CodeReview.Core.Rules;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MattEland.CodeReview.Core;

/// <summary>
/// Extension methods for configuring code review services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds code review services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCodeReview(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<CodeReviewOptions>(configuration.GetSection(CodeReviewOptions.SectionName));
        
        // Core services
        services.AddSingleton<IGitService, GitService>();
        services.AddSingleton<IPromptLoader, PromptLoader>();
        
        // Rule system
        services.AddSingleton<IRuleSource, EmbeddedRuleSource>();
        services.AddSingleton<IRuleSource, FileSystemRuleSource>();
        services.AddSingleton<RuleProvider>();
        services.AddSingleton<IRuleProvider>(sp => sp.GetRequiredService<RuleProvider>());
        
        // AI client factory
        services.AddSingleton<ChatClientFactory>();
        
        // Analysis service
        services.AddSingleton<ICodeReviewService, CodeReviewService>();
        
        return services;
    }

    /// <summary>
    /// Initializes the code review system by loading rules from all sources.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task InitializeCodeReviewAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var ruleProvider = services.GetRequiredService<RuleProvider>();
        await ruleProvider.InitializeAsync(cancellationToken);
    }
}
