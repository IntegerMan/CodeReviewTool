using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Tests.Models;

public class ReviewResultTests
{
    [Fact]
    public void GetSeveritySummary_CountsCorrectly()
    {
        // Arrange
        var result = CreateResult(
            new Issue { Id = "1", ProfileId = "p1", FilePath = "a.cs", Severity = Severity.Critical, Message = "M" },
            new Issue { Id = "2", ProfileId = "p1", FilePath = "a.cs", Severity = Severity.Critical, Message = "M" },
            new Issue { Id = "3", ProfileId = "p2", FilePath = "b.cs", Severity = Severity.Warning, Message = "M" },
            new Issue { Id = "4", ProfileId = "p3", FilePath = "c.cs", Severity = Severity.Info, Message = "M" }
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
            new Issue { Id = "1", ProfileId = "p1", FilePath = "a.cs", Severity = Severity.Critical, Message = "M" },
            new Issue { Id = "2", ProfileId = "p1", FilePath = "a.cs", Severity = Severity.Critical, Message = "M" },
            new Issue { Id = "3", ProfileId = "p2", FilePath = "b.cs", Severity = Severity.Warning, Message = "M" }
        );

        Assert.Equal(2, result.CriticalCount);
    }

    [Fact]
    public void GetIssuesForFile_FiltersCorrectly()
    {
        var result = CreateResult(
            new Issue { Id = "1", ProfileId = "p1", FilePath = "a.cs", Severity = Severity.Warning, Message = "M" },
            new Issue { Id = "2", ProfileId = "p1", FilePath = "a.cs", Severity = Severity.Warning, Message = "M" },
            new Issue { Id = "3", ProfileId = "p2", FilePath = "b.cs", Severity = Severity.Warning, Message = "M" }
        );

        var aIssues = result.GetIssuesForFile("a.cs").ToList();

        Assert.Equal(2, aIssues.Count);
        Assert.All(aIssues, i => Assert.Equal("a.cs", i.FilePath));
    }

    [Fact]
    public void GetIssuesByProfile_FiltersCorrectly()
    {
        var result = CreateResult(
            new Issue { Id = "1", ProfileId = "profile1", FilePath = "a.cs", Severity = Severity.Warning, Message = "M" },
            new Issue { Id = "2", ProfileId = "profile1", FilePath = "b.cs", Severity = Severity.Warning, Message = "M" },
            new Issue { Id = "3", ProfileId = "profile2", FilePath = "c.cs", Severity = Severity.Warning, Message = "M" }
        );

        var profile1Issues = result.GetIssuesByProfile("profile1").ToList();

        Assert.Equal(2, profile1Issues.Count);
        Assert.All(profile1Issues, i => Assert.Equal("profile1", i.ProfileId));
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
            AppliedProfiles = []
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
            AppliedProfiles = []
        };
    }

    private static GitDiff CreateEmptyDiff() => new()
    {
        BaseRef = "main",
        HeadRef = "feature",
        Files = []
    };
}
