using System.Text.RegularExpressions;
using MattEland.CodeReview.Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MattEland.CodeReview.Core.Prompts;

/// <summary>
/// Implementation of <see cref="IPromptLoader"/> that parses .prompt files.
/// </summary>
public sealed partial class PromptLoader : IPromptLoader
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <inheritdoc />
    public async Task<Rule> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return LoadFromContent(filePath, content);
    }

    /// <inheritdoc />
    public Rule LoadFromContent(string resourceName, string content)
    {
        var (frontmatter, promptBody) = ParseFrontmatter(content);
        
        if (frontmatter == null)
        {
            throw new InvalidOperationException($"Invalid .prompt file format: missing frontmatter in {resourceName}");
        }

        var metadata = YamlDeserializer.Deserialize<PromptMetadata>(frontmatter);
        
        // Derive language and category from ID if not explicitly set
        var idParts = metadata.Id?.Split('/') ?? [];
        var language = idParts.Length > 0 ? idParts[0] : "unknown";
        var category = idParts.Length > 1 ? idParts[1] : "general";

        return new Rule
        {
            Id = metadata.Id ?? Path.GetFileNameWithoutExtension(resourceName),
            Name = metadata.Name ?? Path.GetFileNameWithoutExtension(resourceName),
            Description = metadata.Description ?? string.Empty,
            Language = language,
            Category = category,
            DefaultSeverity = ParseSeverity(metadata.Severity),
            Tags = metadata.Tags ?? [],
            PromptPath = resourceName,
            PromptContent = promptBody.Trim(),
            IsEmbedded = resourceName.StartsWith("MattEland.CodeReview")
        };
    }

    /// <inheritdoc />
    public string RenderPrompt(Rule rule, PromptContext context)
    {
        if (string.IsNullOrEmpty(rule.PromptContent))
        {
            throw new InvalidOperationException($"Rule '{rule.Id}' has no prompt content");
        }

        var rendered = rule.PromptContent;
        
        // Replace standard placeholders
        rendered = rendered.Replace("{{diff}}", context.Diff);
        rendered = rendered.Replace("{{file_path}}", context.FilePath ?? string.Empty);
        rendered = rendered.Replace("{{language}}", context.Language ?? string.Empty);
        
        // Replace additional values
        foreach (var (key, value) in context.AdditionalValues)
        {
            rendered = rendered.Replace($"{{{{{key}}}}}", value);
        }

        return rendered;
    }

    private static (string? Frontmatter, string Body) ParseFrontmatter(string content)
    {
        var match = FrontmatterRegex().Match(content);
        
        if (!match.Success)
        {
            return (null, content);
        }

        var frontmatter = match.Groups["frontmatter"].Value;
        var body = match.Groups["body"].Value;
        
        return (frontmatter, body);
    }

    private static Severity ParseSeverity(string? severity) => severity?.ToLowerInvariant() switch
    {
        "info" => Severity.Info,
        "warning" => Severity.Warning,
        "error" => Severity.Error,
        "critical" => Severity.Critical,
        _ => Severity.Warning
    };

    [GeneratedRegex(@"^---\s*\r?\n(?<frontmatter>.*?)\r?\n---\s*\r?\n(?<body>.*)", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();
}

/// <summary>
/// Metadata from the YAML frontmatter of a .prompt file.
/// </summary>
internal sealed class PromptMetadata
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Severity { get; set; }
    public List<string>? Tags { get; set; }
}
