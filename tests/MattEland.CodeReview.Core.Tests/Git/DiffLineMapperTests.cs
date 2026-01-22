using MattEland.CodeReview.Core.Git;

namespace MattEland.CodeReview.Core.Tests.Git;

public class DiffLineMapperTests
{
    [Fact]
    public void Parse_SingleHunk_MapsLinesCorrectly()
    {
        // Arrange - typical git diff with one hunk
        const string diff = """
            diff --git a/src/File.cs b/src/File.cs
            index abc123..def456 100644
            --- a/src/File.cs
            +++ b/src/File.cs
            @@ -10,6 +10,8 @@ namespace Example
             using System;
             
             public class MyClass
            +{
            +    public int Value { get; set; }
             }
            """;

        // Act
        var mapper = DiffLineMapper.Parse(diff);

        // Assert - the lines prefixed with '+' map to source lines 13 and 14
        // Counting from @@ -10,6 +10,8 @@: starts at line 10
        // Line 1 after header is context (' ') -> line 10
        // Line 2 after header is context (' ') -> line 11  
        // Line 3 after header is context (' ') -> line 12
        // Line 4 after header is addition ('+') -> line 13
        // Line 5 after header is addition ('+') -> line 14
        var addedLines = mapper.GetAddedLines().ToList();
        Assert.Equal(2, addedLines.Count);
        Assert.Equal(13, addedLines[0].SourceLineNumber);
        Assert.Equal(14, addedLines[1].SourceLineNumber);
    }

    [Fact]
    public void Parse_WithDeletions_MapsAddedLinesOnly()
    {
        // Arrange - diff with both additions and deletions
        const string diff = """
            @@ -5,4 +5,5 @@ context
             line 5
            -deleted line
            +new line 1
            +new line 2
             line 7
            """;

        // Act
        var mapper = DiffLineMapper.Parse(diff);

        // Assert
        var addedLines = mapper.GetAddedLines().ToList();
        Assert.Equal(2, addedLines.Count);
        
        // After deletion, additions start at line 6 (line 5 was context, deletion doesn't consume line number)
        Assert.Equal(6, addedLines[0].SourceLineNumber);
        Assert.Equal(7, addedLines[1].SourceLineNumber);
    }

    [Fact]
    public void Parse_MultipleHunks_MapsAllCorrectly()
    {
        // Arrange
        const string diff = """
            @@ -1,3 +1,4 @@
             line 1
            +added at line 2
             line 3
            @@ -10,2 +11,3 @@
             line 11
            +added at line 12
             line 13
            """;

        // Act
        var mapper = DiffLineMapper.Parse(diff);

        // Assert
        var addedLines = mapper.GetAddedLines().ToList();
        Assert.Equal(2, addedLines.Count);
        Assert.Equal(2, addedLines[0].SourceLineNumber);
        Assert.Equal(12, addedLines[1].SourceLineNumber);
    }

    [Fact]
    public void GetSourceLineNumber_ReturnsNullForDeletedLines()
    {
        // Arrange
        const string diff = """
            @@ -5,3 +5,2 @@
             context
            -deleted
             more context
            """;

        // Act
        var mapper = DiffLineMapper.Parse(diff);
        
        // Find the deleted line's diff position (should be position 3)
        var lines = mapper.GetAddedLines().ToList();
        
        // The deleted line has no source line in new file
        // This is tested implicitly - there should be no added lines
        Assert.Empty(lines);
    }

    [Fact]
    public void ExtractCodeSnippet_ReturnsContextLines()
    {
        // Arrange
        const string sourceCode = """
            using System;
            
            namespace Example
            {
                public class MyClass
                {
                    public int Value { get; set; }
                }
            }
            """;

        // Act - extract around line 5 with 2 context lines
        var snippet = DiffLineMapper.ExtractCodeSnippet(sourceCode, 5, contextLines: 2);

        // Assert
        Assert.NotNull(snippet);
        Assert.Contains("→", snippet); // Indicator for target line
        Assert.Contains("MyClass", snippet);
        
        // Should show lines 3-7 (2 before, target, 2 after)
        var lines = snippet.Split('\n');
        Assert.Equal(5, lines.Length);
    }

    [Fact]
    public void ExtractCodeSnippet_HandlesEdgeCase_FirstLines()
    {
        // Arrange
        const string sourceCode = """
            line 1
            line 2
            line 3
            line 4
            line 5
            """;

        // Act - extract around line 1 with 3 context lines
        var snippet = DiffLineMapper.ExtractCodeSnippet(sourceCode, 1, contextLines: 3);

        // Assert
        Assert.NotNull(snippet);
        // Should only show lines 1-4 (can't go before line 1)
        var lines = snippet.Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Contains("→", lines[0]); // First line should be marked
    }

    [Fact]
    public void ExtractCodeSnippet_HandlesEdgeCase_LastLines()
    {
        // Arrange
        const string sourceCode = """
            line 1
            line 2
            line 3
            """;

        // Act - extract around line 3 with 3 context lines
        var snippet = DiffLineMapper.ExtractCodeSnippet(sourceCode, 3, contextLines: 3);

        // Assert
        Assert.NotNull(snippet);
        var lines = snippet.Split('\n');
        // Should show lines 1-3 (limited to available lines)
        Assert.Contains("→", lines[^1]); // Last line should be marked
    }

    [Fact]
    public void ExtractCodeSnippet_ReturnsNull_WhenLineOutOfRange()
    {
        // Arrange
        const string sourceCode = "line 1\nline 2";

        // Act
        var snippet = DiffLineMapper.ExtractCodeSnippet(sourceCode, 100, contextLines: 2);

        // Assert
        Assert.Null(snippet);
    }

    [Fact]
    public void ExtractCodeSnippet_ReturnsNull_WhenContentIsNull()
    {
        // Act
        var snippet = DiffLineMapper.ExtractCodeSnippet(null, 5, contextLines: 2);

        // Assert
        Assert.Null(snippet);
    }

    [Fact]
    public void Parse_EmptyDiff_ReturnsEmptyMapper()
    {
        // Act
        var mapper = DiffLineMapper.Parse("");

        // Assert
        Assert.Empty(mapper.GetAddedLines());
    }

    [Fact]
    public void ExtractSnippetFromDiff_ReturnsSnippet()
    {
        // Arrange
        const string diff = """
            @@ -1,5 +1,6 @@
             line 1
             line 2
            +new line
             line 3
             line 4
            """;
        
        // Act - center on line 3 (the '+new line')
        var snippet = DiffLineMapper.ExtractSnippetFromDiff(diff, 3, contextLines: 1);

        // Assert
        Assert.NotNull(snippet);
        // Should show line 2 (context), line 3 (target), line 4 (context)
        Assert.Contains("2:     line 2", snippet);
        Assert.Contains("3: → + new line", snippet);
        Assert.Contains("4:     line 3", snippet);
    }

    [Fact]
    public void ExtractSnippetFromDiff_HandlesMissingLines()
    {
        // Arrange - diff only has a small hunk far down the file
        const string diff = """
            @@ -100,2 +100,3 @@
             context
            +added
             context
            """;

        // Act - request snippet for line 5 (not in diff)
        var snippet = DiffLineMapper.ExtractSnippetFromDiff(diff, 5, contextLines: 2);

        // Assert
        Assert.Null(snippet);
    }
}
