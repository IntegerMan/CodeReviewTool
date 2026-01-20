using LibGit2Sharp;
using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Git;

/// <summary>
/// Implementation of <see cref="IGitService"/> using LibGit2Sharp.
/// </summary>
public sealed class GitService : IGitService
{
    /// <inheritdoc />
    public Task<GitDiff> GetDiffFromBaseAsync(
        string repositoryPath,
        string baseBranch = "main",
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var repo = new Repository(repositoryPath);
            
            var baseBranchRef = repo.Branches[baseBranch] 
                ?? throw new InvalidOperationException($"Branch '{baseBranch}' not found");
            
            var baseCommit = baseBranchRef.Tip;
            var allChanges = new Dictionary<string, PatchEntryChanges>();
            
            // First, get committed changes between base branch and current HEAD
            var headCommit = repo.Head.Tip;
            if (headCommit != null && baseCommit != null)
            {
                var committedDiff = repo.Diff.Compare<Patch>(baseCommit.Tree, headCommit.Tree);
                foreach (var entry in committedDiff)
                {
                    allChanges[entry.Path] = entry;
                }
            }
            
            // Then, include uncommitted working directory changes (staged + unstaged)
            // Compare base branch to working directory to capture everything
            var workingDiff = repo.Diff.Compare<Patch>(baseCommit?.Tree, DiffTargets.Index | DiffTargets.WorkingDirectory);
            foreach (var entry in workingDiff)
            {
                // Working directory changes take precedence (they're more recent)
                allChanges[entry.Path] = entry;
            }
            
            return CreateGitDiff(allChanges.Values, baseBranch, repo.Head.FriendlyName + " (with working changes)");
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<GitDiff> GetDiffForFilesAsync(
        string repositoryPath,
        IEnumerable<string> filePaths,
        string baseBranch = "main",
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var repo = new Repository(repositoryPath);
            
            var baseBranchRef = repo.Branches[baseBranch]
                ?? throw new InvalidOperationException($"Branch '{baseBranch}' not found");
            
            var headCommit = repo.Head.Tip;
            var baseCommit = baseBranchRef.Tip;
            
            var filePathSet = filePaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var diff = repo.Diff.Compare<Patch>(baseCommit.Tree, headCommit.Tree);
            
            var filteredFiles = diff
                .Where(entry => filePathSet.Contains(entry.Path) || 
                               (entry.OldPath != null && filePathSet.Contains(entry.OldPath)))
                .ToList();
            
            return CreateGitDiff(filteredFiles, baseBranch, repo.Head.FriendlyName);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public string GetCurrentBranch(string repositoryPath)
    {
        using var repo = new Repository(repositoryPath);
        return repo.Head.FriendlyName;
    }

    /// <inheritdoc />
    public bool IsValidRepository(string path)
    {
        return Repository.IsValid(path);
    }

    /// <inheritdoc />
    public string? GetRepositoryRoot(string path)
    {
        var repoPath = Repository.Discover(path);
        if (string.IsNullOrEmpty(repoPath))
            return null;
            
        using var repo = new Repository(repoPath);
        return repo.Info.WorkingDirectory?.TrimEnd(Path.DirectorySeparatorChar);
    }

    private static GitDiff CreateGitDiff(IEnumerable<PatchEntryChanges> entries, string baseRef, string headRef)
    {
        var files = entries.Select(entry => new FileChange
        {
            Path = entry.Path,
            OldPath = entry.OldPath != entry.Path ? entry.OldPath : null,
            ChangeType = MapChangeType(entry.Status),
            DiffContent = entry.Patch,
            LinesAdded = entry.LinesAdded,
            LinesDeleted = entry.LinesDeleted,
            Language = DetectLanguage(entry.Path)
        }).ToList();

        return new GitDiff
        {
            BaseRef = baseRef,
            HeadRef = headRef,
            Files = files
        };
    }

    private static FileChangeType MapChangeType(ChangeKind changeKind) => changeKind switch
    {
        ChangeKind.Added => FileChangeType.Added,
        ChangeKind.Deleted => FileChangeType.Deleted,
        ChangeKind.Modified => FileChangeType.Modified,
        ChangeKind.Renamed => FileChangeType.Renamed,
        ChangeKind.Copied => FileChangeType.Copied,
        _ => FileChangeType.Modified
    };

    private static string? DetectLanguage(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".sql" => "sql",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".py" => "python",
            ".java" => "java",
            ".go" => "go",
            ".rb" => "ruby",
            ".rs" => "rust",
            ".cpp" or ".cc" or ".cxx" or ".hpp" or ".h" => "cpp",
            ".c" => "c",
            ".fs" or ".fsx" => "fsharp",
            ".vb" => "vb",
            ".xml" or ".csproj" or ".fsproj" or ".vbproj" => "xml",
            ".json" => "json",
            ".yaml" or ".yml" => "yaml",
            ".md" => "markdown",
            ".html" or ".htm" => "html",
            ".css" => "css",
            ".scss" or ".sass" => "scss",
            ".sh" or ".bash" => "shell",
            ".ps1" or ".psm1" => "powershell",
            _ => null
        };
    }
}
