# ADR 0003: Prompt File Format (.prompt)

## Status

Accepted

## Context

Rules need to store both metadata (name, severity, tags) and prompt content (instructions for the LLM). We need a file format that:

- Is human-readable and editable
- Supports structured metadata
- Allows rich prompt content with formatting
- Can be version controlled effectively

## Decision

We will use `.prompt` files with YAML frontmatter and Markdown body, similar to Jekyll/Hugo content files and Prompty format.

### File Structure

```yaml
---
id: sql/performance/non-sargable
name: Non-Sargable Predicates
description: Detects function calls on indexed columns that prevent index usage
severity: warning
tags: [performance, indexing, sql-caffeine]
---
## Role
You are a SQL performance expert analyzing code changes.

## Detection Target
Find WHERE clauses that wrap indexed columns in functions:
- YEAR(date_column) = 2024
- UPPER(name) = 'VALUE'
- CONVERT(VARCHAR, id) = '123'

## Input
Analyze this SQL diff:
{{diff}}

## Output Format
Return a JSON array of issues found with the following structure:
- line: Line number in the diff
- message: Description of the issue
- suggestion: How to fix it
```

### Frontmatter Fields

| Field | Required | Description |
|-------|----------|-------------|
| `id` | Yes | Unique rule identifier (path-based) |
| `name` | Yes | Human-readable display name |
| `description` | Yes | Brief description of what the rule detects |
| `severity` | Yes | `info`, `warning`, `error`, or `critical` |
| `tags` | No | Array of tags for filtering/grouping |

### Template Variables

- `{{diff}}`: The git diff content to analyze
- `{{file_path}}`: Path of the file being analyzed
- `{{language}}`: Programming language of the file

## Consequences

### Positive

- Familiar format for developers (Jekyll, Hugo, Prompty)
- Metadata and content in single file
- Easy to read/edit in any text editor
- Git-friendly for version control
- YamlDotNet provides robust parsing

### Negative

- Two parsers needed (YAML + Markdown separator)
- Frontmatter delimiter (`---`) must be exact
- Large prompts may be unwieldy in single file

## Implementation Notes

Parse files by:
1. Finding first `---` after start of file
2. Reading until second `---`
3. Parsing that section as YAML
4. Everything after second `---` is the prompt body

## References

- [Prompty Specification](https://github.com/microsoft/prompty)
- [Jekyll Front Matter](https://jekyllrb.com/docs/front-matter/)
- [GitHub Copilot Prompt Files](https://docs.github.com/en/copilot/tutorials/customization-library/prompt-files)
