using System.Text.Json;
using MattEland.CodeReview.Core;
using MattEland.CodeReview.Core.Analysis;
using MattEland.CodeReview.Core.Models;
using MattEland.CodeReview.Core.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MattEland.CodeReview.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables("CODEREVIEW_")
            .Build();

        // Build services
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCodeReview(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Initialize rule provider
            await serviceProvider.InitializeCodeReviewAsync();

            // Parse command
            var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

            return command switch
            {
                "analyze" => await RunAnalyze(args, serviceProvider, configuration),
                "rules" => RunListRules(args, serviceProvider),
                "version" => RunVersion(),
                "help" or "--help" or "-h" => RunHelp(),
                _ => RunHelp()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Command failed");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return -1;
        }
    }

    private static async Task<int> RunAnalyze(string[] args, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var reviewService = serviceProvider.GetRequiredService<ICodeReviewService>();
        
        // Parse options
        var repoPath = GetArgValue(args, "--path", "-p") ?? Directory.GetCurrentDirectory();
        var baseBranch = GetArgValue(args, "--base", "-b") ?? configuration["CodeReview:Git:DefaultBaseBranch"] ?? "main";
        var files = GetArgValue(args, "--files", "-f");
        var output = GetArgValue(args, "--output", "-o") ?? "console";
        var verbose = HasFlag(args, "--verbose", "-v");

        if (verbose)
        {
            Console.WriteLine($"Analyzing repository: {repoPath}");
            Console.WriteLine($"Base branch: {baseBranch}");
        }

        ReviewResult result;
        if (!string.IsNullOrEmpty(files))
        {
            var filePaths = files.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            result = await reviewService.AnalyzeFilesAsync(repoPath, filePaths, baseBranch);
        }
        else
        {
            result = await reviewService.AnalyzeDiffAsync(repoPath, baseBranch);
        }

        OutputResult(result, output, verbose);
        
        return result.CriticalCount > 0 ? 2 : (result.ErrorCount > 0 ? 1 : 0);
    }

    private static int RunListRules(string[] args, IServiceProvider serviceProvider)
    {
        var ruleProvider = serviceProvider.GetRequiredService<IRuleProvider>();
        var language = GetArgValue(args, "--language", "-l");
        
        var rules = string.IsNullOrEmpty(language) 
            ? ruleProvider.GetAllRules()
            : ruleProvider.GetRulesByLanguage(language);

        Console.WriteLine();
        Console.WriteLine("Available Rules:");
        Console.WriteLine(new string('=', 80));
        
        foreach (var group in rules.GroupBy(r => r.Language).OrderBy(g => g.Key))
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  {group.Key.ToUpperInvariant()}");
            Console.ResetColor();
            
            foreach (var categoryGroup in group.GroupBy(r => r.Category).OrderBy(g => g.Key))
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"    {categoryGroup.Key}/");
                Console.ResetColor();
                
                foreach (var rule in categoryGroup.OrderBy(r => r.Name))
                {
                    var severityColor = rule.DefaultSeverity switch
                    {
                        Severity.Critical => ConsoleColor.Red,
                        Severity.Error => ConsoleColor.DarkRed,
                        Severity.Warning => ConsoleColor.Yellow,
                        _ => ConsoleColor.Gray
                    };
                    
                    Console.Write("      ");
                    Console.ForegroundColor = severityColor;
                    Console.Write($"[{rule.DefaultSeverity.ToString()[0]}]");
                    Console.ResetColor();
                    Console.WriteLine($" {rule.Name}");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"          {rule.Description}");
                    Console.ResetColor();
                }
            }
        }
        Console.WriteLine();
        return 0;
    }

    private static int RunVersion()
    {
        var version = typeof(Program).Assembly.GetName().Version;
        Console.WriteLine($"MattEland.CodeReview CLI v{version}");
        Console.WriteLine($".NET {Environment.Version}");
        return 0;
    }

    private static int RunHelp()
    {
        Console.WriteLine("""
            MattEland.CodeReview - LLM-powered code review tool

            Usage: codereview <command> [options]

            Commands:
              analyze     Analyze git diff for code issues
              rules       List available analysis rules
              version     Show version information
              help        Show this help message

            Analyze Options:
              --path, -p <path>       Path to the git repository (default: current directory)
              --base, -b <branch>     Base branch to compare against (default: main)
              --files, -f <files>     Comma-separated list of specific files to analyze
              --output, -o <format>   Output format: console, json, or markdown (default: console)
              --verbose, -v           Show detailed output

            Rules Options:
              --language, -l <lang>   Filter rules by language (csharp, sql)

            Examples:
              codereview analyze
              codereview analyze --base develop --output json
              codereview analyze --files "src/Service.cs,scripts/migration.sql"
              codereview rules --language sql
            """);
        return 0;
    }

    private static string? GetArgValue(string[] args, string longName, string shortName)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == longName || args[i] == shortName)
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static bool HasFlag(string[] args, string longName, string shortName)
    {
        return args.Contains(longName) || args.Contains(shortName);
    }

    private static void OutputResult(ReviewResult result, string format, bool verbose)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                OutputJson(result);
                break;
            case "markdown":
            case "md":
                OutputMarkdown(result);
                break;
            default:
                OutputConsole(result, verbose);
                break;
        }
    }

    private static void OutputJson(ReviewResult result)
    {
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        Console.WriteLine(JsonSerializer.Serialize(result, options));
    }

    private static void OutputMarkdown(ReviewResult result)
    {
        Console.WriteLine($"# Code Review Results");
        Console.WriteLine();
        Console.WriteLine($"**Review ID:** {result.Id}");
        Console.WriteLine($"**Duration:** {result.Duration?.TotalSeconds:F2}s");
        Console.WriteLine($"**Files Analyzed:** {result.Diff.Files.Count}");
        Console.WriteLine();
        
        Console.WriteLine("## Summary");
        Console.WriteLine();
        Console.WriteLine($"| Severity | Count |");
        Console.WriteLine($"|----------|-------|");
        Console.WriteLine($"| Critical | {result.CriticalCount} |");
        Console.WriteLine($"| Error | {result.ErrorCount} |");
        Console.WriteLine($"| Warning | {result.WarningCount} |");
        Console.WriteLine($"| Info | {result.InfoCount} |");
        Console.WriteLine();
        
        if (result.Issues.Count > 0)
        {
            Console.WriteLine("## Issues");
            Console.WriteLine();
            
            foreach (var group in result.Issues.GroupBy(i => i.FilePath))
            {
                Console.WriteLine($"### {group.Key}");
                Console.WriteLine();
                
                foreach (var issue in group.OrderBy(i => i.StartLine))
                {
                    Console.WriteLine($"- **{issue.RuleId}** [{issue.Severity}] (Line {issue.StartLine})");
                    Console.WriteLine($"  - {issue.Message}");
                    if (!string.IsNullOrEmpty(issue.Suggestion))
                    {
                        Console.WriteLine($"  - Suggestion: {issue.Suggestion}");
                    }
                    Console.WriteLine();
                }
            }
        }
        else
        {
            Console.WriteLine("## No issues found!");
        }
    }

    private static void OutputConsole(ReviewResult result, bool verbose)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  Code Review Results");
        Console.ResetColor();
        Console.WriteLine($"  {new string('-', 50)}");
        Console.WriteLine($"  Review ID: {result.Id}");
        Console.WriteLine($"  Duration:  {result.Duration?.TotalSeconds:F2}s");
        Console.WriteLine($"  Files:     {result.Diff.Files.Count}");
        Console.WriteLine($"  Rules:     {result.AppliedRules.Count}");
        Console.WriteLine();
        
        Console.Write("  Summary: ");
        if (result.CriticalCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{result.CriticalCount} critical ");
        }
        if (result.ErrorCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write($"{result.ErrorCount} errors ");
        }
        if (result.WarningCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{result.WarningCount} warnings ");
        }
        if (result.InfoCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"{result.InfoCount} info ");
        }
        if (result.Issues.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("No issues found!");
        }
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine();
        
        if (result.Issues.Count > 0)
        {
            foreach (var group in result.Issues.GroupBy(i => i.FilePath))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  {group.Key}");
                Console.ResetColor();
                
                foreach (var issue in group.OrderByDescending(i => i.Severity).ThenBy(i => i.StartLine))
                {
                    var (color, symbol) = issue.Severity switch
                    {
                        Severity.Critical => (ConsoleColor.Red, "X"),
                        Severity.Error => (ConsoleColor.DarkRed, "X"),
                        Severity.Warning => (ConsoleColor.Yellow, "!"),
                        _ => (ConsoleColor.Gray, "i")
                    };
                    
                    Console.ForegroundColor = color;
                    Console.Write($"    {symbol} ");
                    Console.ResetColor();
                    
                    var lineInfo = issue.StartLine.HasValue ? $"[{issue.StartLine}] " : "";
                    Console.WriteLine($"{lineInfo}{issue.Message}");
                    
                    if (verbose && !string.IsNullOrEmpty(issue.Suggestion))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine($"      -> {issue.Suggestion}");
                        Console.ResetColor();
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
