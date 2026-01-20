# Architectural Decision Records

This directory contains Architectural Decision Records (ADRs) documenting significant technical decisions made during the development of the MattEland.CodeReview tool.

## What is an ADR?

An Architectural Decision Record captures a single architectural decision, including the context, the decision itself, and the consequences. ADRs help future developers (including your future self) understand why certain choices were made.

## ADR Index

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-use-microsoft-extensions-ai-for-llm-abstraction.md) | Use Microsoft.Extensions.AI for LLM Abstraction | Accepted |
| [0002](0002-hierarchical-rule-organization.md) | Hierarchical Rule Organization | Accepted |
| [0003](0003-prompt-file-format.md) | Prompt File Format (.prompt) | Accepted |
| [0004](0004-uno-platform-for-desktop-ui.md) | Uno Platform for Desktop UI | Accepted |
| [0005](0005-libgit2sharp-for-git-operations.md) | LibGit2Sharp for Git Operations | Accepted |
| [0006](0006-focus-on-logic-errors-beyond-sonarqube.md) | Focus on Logic Errors Beyond SonarQube | Accepted |

## Creating New ADRs

1. Copy the template below
2. Number sequentially (e.g., `0007-*.md`)
3. Fill in all sections
4. Add to the index above

### ADR Template

```markdown
# ADR NNNN: Title

## Status

Proposed | Accepted | Deprecated | Superseded by [ADR XXXX](XXXX-*.md)

## Context

What is the issue that we're seeing that is motivating this decision or change?

## Decision

What is the change that we're proposing and/or doing?

## Consequences

What becomes easier or more difficult to do because of this change?

## References

- Links to relevant resources
```

## References

- [ADR GitHub Organization](https://adr.github.io/)
- [Michael Nygard's Article on ADRs](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
