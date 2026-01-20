using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Tests.Models;

public class ReviewResultTests
{
    [Fact]
    public void GetSeveritySummary_CountsCorrectly()
    {
        // Arrange
        var result = CreateResult(
            new Issue { Id = "1", RuleId = "r1", FilePath = "a.cs", Severity = Severity.Critical, Message = "M" },
            new Issue { Id = "2", RuleId = "r1", FilePath = "a.cs", Severity = Severity.Critical, Message = "M" },
            new Issue { Id = "3", RuleId = "r2", FilePath = "b.cs", Severity = Severity.Warning, Message = "M" },
            new Issue { Id = "4", RuleId = "r3", FilePath = "c.cs", Severity = Severity.Info, Message = "M" }
        );

        // Act
        var summary = result.GetSeveritySummary();

        // Assert
        Assert.Equal(2, summary[Severity.Critical]);
        Assert.Equal(1, summary[Severity.Warning]);
        Assert.Equal(1, summary[Severity.Info]);
        Assert.False(summary.ContainsKey(Severity.Error));
    }

    [Fact]
    public void CriticalCount_ReturnsCorrectCount()
    {
        var result = CreateResult(
            new Issue { Id = "1", RuleId = "r1", FilePath = "a.cs", Severity = Severity.Critical, Message = "M" },
            new Issue { Id = "2", RuleId = "r1", FilePath = "a.cs", Severity = Severity.Critical, Message = "M" },
            new Issue { Id = "3", RuleId = "r2", FilePath = "b.cs", Severity = Severity.Warning, Message = "M" }
        );

        Assert.Equal(2, result.CriticalCount);
    }

    [Fact]
    public void GetIssuesForFile_FiltersCorrectly()
    {
        var result = CreateResult(
            new Issue { Id = "1", RuleId = "r1", FilePath = "a.cs", Severity = Severity.Warning, Message = "M" },
            new Issue { Id = "2", RuleId = "r1", FilePath = "a.cs", Severity = Severity.Warning, Message = "M" },
            new Issue { Id = "3", RuleId = "r2", FilePath = "b.cs", Severity = Severity.Warning, Message = "M" }
        );

        var aIssues = result.GetIssuesForFile("a.cs").ToList();

        Assert.Equal(2, aIssues.Count);
        Assert.All(aIssues, i => Assert.Equal("a.cs", i.FilePath));
    }

    [Fact]
    public void GetIssuesByRule_FiltersCorrectly()
    {
        var result = CreateResult(
            new Issue { Id = "1", RuleId = "rule1", FilePath = "a.cs", Severity = Severity.Warning, Message = "M" },
            new Issue { Id = "2", RuleId = "rule1", FilePath = "b.cs", Severity = Severity.Warning, Message = "M" },
            new Issue { Id = "3", RuleId = "rule2", FilePath = "c.cs", Severity = Severity.Warning, Message = "M" }
        );

        var rule1Issues = result.GetIssuesByRule("rule1").ToList();

        Assert.Equal(2, rule1Issues.Count);
        Assert.All(rule1Issues, i => Assert.Equal("rule1", i.RuleId));
    }

    [Fact]
    public void Duration_CalculatesCorrectly()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddSeconds(5.5);

        var result = new ReviewResult
        {
            Id = "test",
            StartedAt = start,
            CompletedAt = end,
            Diff = CreateEmptyDiff(),
            Issues = [],
            AppliedRules = []
        };

        Assert.NotNull(result.Duration);
        Assert.Equal(5.5, result.Duration.Value.TotalSeconds, precision: 1);
    }

    private static ReviewResult CreateResult(params Issue[] issues)
    {
        return new ReviewResult
        {
            Id = "test",
            StartedAt = DateTimeOffset.UtcNow,
            Diff = CreateEmptyDiff(),
            Issues = issues,
            AppliedRules = []
        };
    }

    private static GitDiff CreateEmptyDiff() => new()
    {
        BaseRef = "main",
        HeadRef = "feature",
        Files = []
    };
}
