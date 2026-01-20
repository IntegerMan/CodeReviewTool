# ADR 0002: Hierarchical Rule Organization

## Status

Accepted

## Context

The code review tool needs to manage multiple analysis rules for different languages (C#, SQL) and concern areas (security, performance, logic). Users need to:

- Enable/disable rules by category
- Add custom rules without modifying core code
- Understand rule organization at a glance

## Decision

We will organize rules in a three-level hierarchy: **Language > Category > Rule**

```
rules/
├── csharp/
│   ├── security/
│   │   ├── hardcoded-secrets.prompt
│   │   └── insecure-deserialization.prompt
│   ├── performance/
│   │   └── blocking-async.prompt
│   └── logic/
│       └── null-after-check.prompt
└── sql/
    ├── performance/
    │   ├── select-star.prompt
    │   └── non-sargable.prompt
    └── security/
        └── sql-injection.prompt
```

### Rule Identification

Rules are identified by their path: `{language}/{category}/{rule-name}`

Examples:
- `sql/performance/select-star`
- `csharp/security/hardcoded-secrets`

### Rule Sources

1. **Embedded Rules**: Built-in rules shipped with the application
2. **User Rules**: Custom rules loaded from a configuration directory at startup

## Consequences

### Positive

- Intuitive organization matching how developers think about code issues
- Easy bulk enable/disable by language or category
- Clear separation between built-in and user rules
- File system structure mirrors logical structure

### Negative

- Fixed three-level hierarchy may not fit all organizational needs
- Rule IDs are tied to file paths
- Moving rules requires updating references

## References

- [SQL Caffeine Top 10 Anti-Patterns](https://sqlcaffeine.com/)
