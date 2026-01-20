using System.Text.RegularExpressions;

namespace MattEland.CodeReview.Core.Git;

/// <summary>
/// Maps line positions within a git diff to actual source file line numbers.
/// </summary>
public sealed partial class DiffLineMapper
{
    private readonly List<DiffHunk> _hunks = [];
    private readonly List<DiffLine> _lines = [];

    /// <summary>
    /// Parses a git diff and creates a line mapper for it.
    /// </summary>
    public static DiffLineMapper Parse(string diffContent)
    {
        var mapper = new DiffLineMapper();
        
        if (string.IsNullOrEmpty(diffContent))
            return mapper;

        var lines = diffContent.Split('\n');
        DiffHunk? currentHunk = null;
        var currentNewLine = 0;
        var diffLinePosition = 0;

        foreach (var line in lines)
        {
            diffLinePosition++;
            
            // Check for hunk header: @@ -10,5 +12,7 @@ optional context
            var match = HunkHeaderRegex().Match(line);
            if (match.Success)
            {
                var newStart = int.Parse(match.Groups["newStart"].Value);
                var newCount = match.Groups["newCount"].Success 
                    ? int.Parse(match.Groups["newCount"].Value) 
                    : 1;
                
                currentHunk = new DiffHunk
                {
                    NewStart = newStart,
                    NewCount = newCount,
                    DiffStartLine = diffLinePosition
                };
                mapper._hunks.Add(currentHunk);
                currentNewLine = newStart;
                continue;
            }

            if (currentHunk == null)
                continue;

            // Track line types and mappings
            if (line.StartsWith('+'))
            {
                // Added line - maps to new file
                mapper._lines.Add(new DiffLine
                {
                    DiffPosition = diffLinePosition,
                    SourceLineNumber = currentNewLine,
                    Type = DiffLineType.Added,
                    Content = line.Length > 1 ? line[1..] : string.Empty
                });
                currentNewLine++;
            }
            else if (line.StartsWith('-'))
            {
                // Deleted line - no mapping to new file
                mapper._lines.Add(new DiffLine
                {
                    DiffPosition = diffLinePosition,
                    SourceLineNumber = null,
                    Type = DiffLineType.Deleted,
                    Content = line.Length > 1 ? line[1..] : string.Empty
                });
            }
            else if (line.StartsWith(' ') || (!line.StartsWith('\\') && currentHunk != null))
            {
                // Context line - maps to new file
                mapper._lines.Add(new DiffLine
                {
                    DiffPosition = diffLinePosition,
                    SourceLineNumber = currentNewLine,
                    Type = DiffLineType.Context,
                    Content = line.StartsWith(' ') && line.Length > 1 ? line[1..] : line
                });
                currentNewLine++;
            }
        }

        return mapper;
    }

    /// <summary>
    /// Gets the source file line number for a given diff line position.
    /// Returns null if the position is not a valid line in the new file.
    /// </summary>
    public int? GetSourceLineNumber(int diffLinePosition)
    {
        var line = _lines.FirstOrDefault(l => l.DiffPosition == diffLinePosition);
        return line?.SourceLineNumber;
    }

    /// <summary>
    /// Finds the closest source line number for a diff position.
    /// If the exact position doesn't map to a source line, finds the nearest one.
    /// </summary>
    public int? GetClosestSourceLineNumber(int diffLinePosition)
    {
        // First try exact match
        var exact = GetSourceLineNumber(diffLinePosition);
        if (exact.HasValue)
            return exact;

        // Find closest line with a source mapping
        var closest = _lines
            .Where(l => l.SourceLineNumber.HasValue)
            .OrderBy(l => Math.Abs(l.DiffPosition - diffLinePosition))
            .FirstOrDefault();

        return closest?.SourceLineNumber;
    }

    /// <summary>
    /// Extracts a code snippet from source content around a given line.
    /// </summary>
    /// <param name="sourceContent">The full source file content.</param>
    /// <param name="lineNumber">The 1-based line number to center on.</param>
    /// <param name="contextLines">Number of lines before and after to include.</param>
    public static string? ExtractCodeSnippet(string? sourceContent, int lineNumber, int contextLines = 3)
    {
        if (string.IsNullOrEmpty(sourceContent) || lineNumber < 1)
            return null;

        var lines = sourceContent.Split('\n');
        if (lineNumber > lines.Length)
            return null;

        var startLine = Math.Max(1, lineNumber - contextLines);
        var endLine = Math.Min(lines.Length, lineNumber + contextLines);

        var snippetLines = new List<string>();
        for (var i = startLine; i <= endLine; i++)
        {
            var prefix = i == lineNumber ? "→ " : "  ";
            var lineContent = lines[i - 1].TrimEnd('\r');
            snippetLines.Add($"{i,4}: {prefix}{lineContent}");
        }

        return string.Join('\n', snippetLines);
    }

    /// <summary>
    /// Gets all lines that were added in this diff.
    /// </summary>
    public IEnumerable<DiffLine> GetAddedLines() => 
        _lines.Where(l => l.Type == DiffLineType.Added);

    [GeneratedRegex(@"^@@ -\d+(?:,\d+)? \+(?<newStart>\d+)(?:,(?<newCount>\d+))? @@")]
    private static partial Regex HunkHeaderRegex();
}

/// <summary>
/// Represents a hunk (section) in a git diff.
/// </summary>
public sealed class DiffHunk
{
    /// <summary>Starting line number in the new file.</summary>
    public required int NewStart { get; init; }
    
    /// <summary>Number of lines in this hunk for the new file.</summary>
    public required int NewCount { get; init; }
    
    /// <summary>Line position in the diff where this hunk starts.</summary>
    public required int DiffStartLine { get; init; }
}

/// <summary>
/// Represents a single line in a diff.
/// </summary>
public sealed class DiffLine
{
    /// <summary>Position of this line in the diff (1-based).</summary>
    public required int DiffPosition { get; init; }
    
    /// <summary>Source file line number (1-based), null for deleted lines.</summary>
    public int? SourceLineNumber { get; init; }
    
    /// <summary>Type of diff line (added, deleted, or context).</summary>
    public required DiffLineType Type { get; init; }
    
    /// <summary>The line content without the diff prefix.</summary>
    public required string Content { get; init; }
}

/// <summary>
/// Type of a line in a diff.
/// </summary>
public enum DiffLineType
{
    /// <summary>Line exists in both old and new versions.</summary>
    Context,
    
    /// <summary>Line was added in the new version.</summary>
    Added,
    
    /// <summary>Line was deleted in the new version.</summary>
    Deleted
}
