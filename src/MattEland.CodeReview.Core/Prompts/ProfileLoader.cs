using System.Text.RegularExpressions;
using MattEland.CodeReview.Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MattEland.CodeReview.Core.Prompts;

/// <summary>
/// Implementation of <see cref="IProfileLoader"/> that parses .profile files.
/// </summary>
public sealed partial class ProfileLoader : IProfileLoader
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <inheritdoc />
    public async Task<ReviewProfile> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return LoadFromContent(filePath, content);
    }

    /// <inheritdoc />
    public ReviewProfile LoadFromContent(string resourceName, string content)
    {
        var (frontmatter, promptBody) = ParseFrontmatter(content);
        
        if (frontmatter == null)
        {
            throw new InvalidOperationException($"Invalid .profile file format: missing frontmatter in {resourceName}");
        }

        var metadata = YamlDeserializer.Deserialize<ProfileMetadata>(frontmatter);

        return new ReviewProfile
        {
            Id = metadata.Id ?? Path.GetFileNameWithoutExtension(resourceName),
            Name = metadata.Name ?? Path.GetFileNameWithoutExtension(resourceName),
            Description = metadata.Description ?? string.Empty,
            ProfilePath = resourceName,
            PromptContent = promptBody.Trim(),
            IsEmbedded = resourceName.StartsWith("MattEland.CodeReview")
        };
    }

    /// <inheritdoc />
    public string RenderPrompt(ReviewProfile profile, PromptContext context)
    {
        if (string.IsNullOrEmpty(profile.PromptContent))
        {
            throw new InvalidOperationException($"Profile '{profile.Id}' has no prompt content");
        }

        var rendered = profile.PromptContent;
        
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

    [GeneratedRegex(@"^---\s*\r?\n(?<frontmatter>.*?)\r?\n---\s*\r?\n(?<body>.*)", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();
}

/// <summary>
/// Metadata from the YAML frontmatter of a .profile file.
/// </summary>
internal sealed class ProfileMetadata
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
