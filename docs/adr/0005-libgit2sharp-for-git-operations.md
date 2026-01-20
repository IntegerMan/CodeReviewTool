# ADR 0005: LibGit2Sharp for Git Operations

## Status

Accepted

## Context

The code review tool needs to:

- Extract diffs between branches
- Identify changed files and their content
- Support repositories with various configurations
- Work without requiring git CLI installation

## Decision

We will use **LibGit2Sharp** for all git operations.

### Reasons

1. **Native .NET**: Pure .NET library with no external dependencies
2. **No CLI Required**: Works without git being installed
3. **Full Repository Access**: Can traverse commits, branches, diffs programmatically
4. **Mature Library**: Widely used, well-documented, stable API
5. **Cross-Platform**: Works on Windows, macOS, and Linux

### Key Operations

```csharp
// Get diff between current branch and main
using var repo = new Repository(repoPath);
var baseBranch = repo.Branches["main"];
var headBranch = repo.Head;

var diff = repo.Diff.Compare<Patch>(
    baseBranch.Tip.Tree,
    headBranch.Tip.Tree
);
```

### Alternatives Considered

1. **Git CLI via Process.Start**: External dependency, parsing stdout is fragile
2. **GitSharp**: Abandoned project, not maintained
3. **NGit**: Java port, less idiomatic .NET
4. **Simple Git Commands**: Requires git installation

## Consequences

### Positive

- Self-contained solution, no external dependencies
- Type-safe API with good IntelliSense
- Efficient native binaries via LibGit2Sharp.NativeBinaries
- Consistent behavior across platforms

### Negative

- Native binaries increase package size (~20MB)
- Some advanced git features may not be exposed
- Must handle repository disposal carefully

### Implementation Notes

- Use `Repository` in a using statement to ensure proper disposal
- Cache repository instance when performing multiple operations
- Handle `RepositoryNotFoundException` for invalid paths
- Consider shallow clone support for CI/CD scenarios

## References

- [LibGit2Sharp GitHub](https://github.com/libgit2/libgit2sharp)
- [LibGit2Sharp Wiki](https://github.com/libgit2/libgit2sharp/wiki)
