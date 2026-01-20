# MattEland.CodeReview

An LLM-powered code review tool that analyzes git diffs for C# and SQL codebases, detecting logic errors, performance issues, and security vulnerabilities that traditional static analysis tools miss.

## Features

- **Git Diff Analysis**: Analyze changes between your current branch and a base branch (defaults to `main`)
- **Multi-Language Support**: Specialized rules for C# and SQL
- **Hierarchical Rule System**: Rules organized by language, category, and specific issue type
- **Multiple AI Providers**: Supports Azure OpenAI, OpenAI, and Ollama via `Microsoft.Extensions.AI`
- **Extensible Prompts**: Built-in rules plus user-defined `.prompt` files loaded at runtime
- **Cross-Platform Desktop App**: Uno Platform-based UI for Windows, macOS, and Linux
- **CLI Tool**: Command-line interface for CI/CD integration

## Project Structure

```
CodeReviewTool/
├── src/
│   ├── MattEland.CodeReview.Core/      # Shared business logic
│   ├── MattEland.CodeReview.Cli/       # Command-line interface
│   └── MattEland.CodeReview.Desktop/   # Uno Platform desktop app
├── tests/
│   └── MattEland.CodeReview.Core.Tests/
├── prompts/                             # Hierarchical .prompt files
│   ├── csharp/
│   │   ├── security/
│   │   ├── performance/
│   │   └── logic/
│   └── sql/
│       ├── performance/
│       ├── security/
│       └── transactions/
└── docs/
    └── adr/                             # Architectural Decision Records
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- One of the following AI providers:
  - Azure OpenAI subscription
  - OpenAI API key
  - [Ollama](https://ollama.ai/) running locally

### Configuration

Create an `appsettings.json` file in the CLI or Desktop project:

```json
{
  "CodeReview": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "DeploymentName": "gpt-4o",
      "ApiKey": "your-api-key"
    },
    "OpenAI": {
      "ApiKey": "your-api-key",
      "Model": "gpt-4o"
    },
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3"
    },
    "Git": {
      "DefaultBaseBranch": "main"
    }
  }
}
```

### CLI Usage

```bash
# Analyze current branch against main
dotnet run --project src/MattEland.CodeReview.Cli -- analyze

# Specify base branch
dotnet run --project src/MattEland.CodeReview.Cli -- analyze --base develop

# Analyze specific files
dotnet run --project src/MattEland.CodeReview.Cli -- analyze --files "src/Service.cs,scripts/migration.sql"

# Output as JSON
dotnet run --project src/MattEland.CodeReview.Cli -- analyze --output json
```

### Desktop App

```bash
# Run the Uno Platform desktop app
dotnet run --project src/MattEland.CodeReview.Desktop/MattEland.CodeReview.Desktop
```

## Built-in Rules

### SQL Rules ([SQL Caffeine](https://sqlcaffeine.com/) Top 10)

| Rule | Description |
|------|-------------|
| `select-star` | SELECT * fetches unnecessary columns |
| `non-sargable` | Functions on columns prevent index usage |
| `type-mismatch` | Implicit type conversions |
| `n-plus-one-where` | Correlated subqueries in WHERE |
| `n-plus-one-select` | Scalar subqueries in SELECT |
| `unnecessary-distinct` | DISTINCT masking join problems |
| `or-conditions` | OR preventing index optimization |
| `not-in-null` | NOT IN with nullable columns |
| `missing-indexes` | Queries on unindexed columns |
| `no-limit` | Unbounded result sets |

### C# Rules

| Category | Rule | Description |
|----------|------|-------------|
| Security | `hardcoded-secrets` | API keys, passwords in code |
| Security | `insecure-deserialization` | Unsafe JSON/XML deserialization |
| Performance | `blocking-async` | `.Result`, `.Wait()` calls |
| Performance | `n-plus-one-linq` | LINQ queries in loops |
| Logic | `null-after-check` | Null dereference issues |
| Logic | `off-by-one` | Loop boundary errors |

## Custom Prompts

Add custom `.prompt` files to `%AppData%/MattEland.CodeReview/prompts/` (Windows) or `~/.config/MattEland.CodeReview/prompts/` (Linux/macOS).

### .prompt File Format

```yaml
---
id: sql/performance/custom-rule
name: Custom Rule Name
description: What this rule detects
severity: warning
tags: [performance, custom]
---
## Role
You are an expert code reviewer...

## Detection Target
Look for patterns that...

## Input
Analyze this diff:
{{diff}}

## Output Format
Return JSON array of issues...
```

## Architecture

See the [Architectural Decision Records](docs/adr/) for design rationale.

## License

MIT License - see [LICENSE](LICENSE) for details.
