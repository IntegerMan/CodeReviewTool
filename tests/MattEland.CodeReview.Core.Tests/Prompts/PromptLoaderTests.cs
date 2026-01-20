using MattEland.CodeReview.Core.Models;
using MattEland.CodeReview.Core.Prompts;

namespace MattEland.CodeReview.Core.Tests.Prompts;

public class PromptLoaderTests
{
    private readonly PromptLoader _loader = new();

    [Fact]
    public void LoadFromContent_ParsesFrontmatter()
    {
        // Arrange
        const string content = """
            ---
            id: sql/performance/test-rule
            name: Test Rule
            description: A test rule for unit testing
            severity: warning
            tags: [test, example]
            ---
            ## Role
            You are a test assistant.
            
            ## Input
            {{diff}}
            """;

        // Act
        var rule = _loader.LoadFromContent("test.prompt", content);

        // Assert
        Assert.Equal("sql/performance/test-rule", rule.Id);
        Assert.Equal("Test Rule", rule.Name);
        Assert.Equal("A test rule for unit testing", rule.Description);
        Assert.Equal(Severity.Warning, rule.DefaultSeverity);
        Assert.Equal("sql", rule.Language);
        Assert.Equal("performance", rule.Category);
        Assert.Contains("test", rule.Tags);
        Assert.Contains("You are a test assistant", rule.PromptContent);
    }

    [Fact]
    public void LoadFromContent_HandlesCriticalSeverity()
    {
        const string content = """
            ---
            id: security/test
            name: Critical Test
            description: Test
            severity: critical
            ---
            Content
            """;

        var rule = _loader.LoadFromContent("test.prompt", content);

        Assert.Equal(Severity.Critical, rule.DefaultSeverity);
    }

    [Fact]
    public void LoadFromContent_DefaultsToWarning_WhenSeverityMissing()
    {
        const string content = """
            ---
            id: test/rule
            name: Test
            description: Test
            ---
            Content
            """;

        var rule = _loader.LoadFromContent("test.prompt", content);

        Assert.Equal(Severity.Warning, rule.DefaultSeverity);
    }

    [Fact]
    public void RenderPrompt_ReplacesDiffPlaceholder()
    {
        const string content = """
            ---
            id: test/rule
            name: Test
            description: Test
            severity: info
            ---
            Analyze this:
            {{diff}}
            Done.
            """;

        var rule = _loader.LoadFromContent("test.prompt", content);
        var context = new PromptContext
        {
            Diff = "--- a/file.cs\n+++ b/file.cs\n+ new line"
        };

        var rendered = _loader.RenderPrompt(rule, context);

        Assert.Contains("--- a/file.cs", rendered);
        Assert.Contains("+ new line", rendered);
        Assert.DoesNotContain("{{diff}}", rendered);
    }

    [Fact]
    public void RenderPrompt_ReplacesFilePathPlaceholder()
    {
        const string content = """
            ---
            id: test/rule
            name: Test
            description: Test
            severity: info
            ---
            File: {{file_path}}
            {{diff}}
            """;

        var rule = _loader.LoadFromContent("test.prompt", content);
        var context = new PromptContext
        {
            Diff = "test diff",
            FilePath = "src/MyClass.cs"
        };

        var rendered = _loader.RenderPrompt(rule, context);

        Assert.Contains("File: src/MyClass.cs", rendered);
    }

    [Fact]
    public void LoadFromContent_ThrowsOnMissingFrontmatter()
    {
        const string content = "No frontmatter here";

        Assert.Throws<InvalidOperationException>(() => 
            _loader.LoadFromContent("bad.prompt", content));
    }
}
