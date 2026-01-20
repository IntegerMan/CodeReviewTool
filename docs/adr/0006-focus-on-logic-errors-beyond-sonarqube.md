# ADR 0006: Focus on Logic Errors Beyond SonarQube

## Status

Accepted

## Context

Static analysis tools like SonarQube already detect many code quality issues:

- Code smells (long methods, complex conditions)
- Simple security patterns (hardcoded passwords in specific formats)
- Naming conventions
- Code duplication

Our LLM-based tool should complement, not duplicate, these capabilities.

## Decision

We will focus analysis rules on issues that require **semantic understanding**—problems that static analysis tools typically miss because they require understanding intent, context, or domain knowledge.

### Priority Areas

#### C# Logic Errors
- **Null handling sequences**: Dereferencing before checking null, or redundant checks
- **Async anti-patterns**: `.Result`, `.Wait()`, `GetAwaiter().GetResult()` in async contexts
- **Off-by-one errors**: Loop boundaries, array access at edges
- **Race conditions**: Shared state without synchronization
- **Business logic**: Calculations that don't match apparent intent

#### SQL Performance (SQL Caffeine Top 10)
Based on [sqlcaffeine.com](https://sqlcaffeine.com/):

1. **SELECT \***: Fetches unnecessary columns
2. **Non-Sargable**: Functions on indexed columns (`YEAR(date) = 2024`)
3. **Type Mismatch**: Implicit conversions (`WHERE total = '25.99'`)
4. **N+1 WHERE**: Correlated subqueries
5. **N+1 SELECT**: Scalar subqueries in SELECT list
6. **Unnecessary DISTINCT**: Masking join problems
7. **OR Conditions**: Preventing index optimization
8. **NOT IN NULL**: Null handling in NOT IN clauses
9. **Missing Indexes**: Queries on unindexed columns
10. **No LIMIT**: Unbounded result sets

#### Security Beyond Patterns
- Context-aware secret detection (variable names + values)
- Authorization bypass patterns
- Insecure deserialization configurations
- SQL injection in dynamic query building

### What We Skip

- Formatting and style (handled by formatters/linters)
- Naming conventions (handled by analyzers)
- Simple null checks (handled by nullable reference types)
- Common code smells (handled by SonarQube)

## Consequences

### Positive

- Provides unique value not available elsewhere
- Reduces noise by not duplicating existing tools
- LLMs excel at semantic understanding
- Aligns with user expectations ("find bugs I'd miss")

### Negative

- More complex prompts required
- Higher chance of false positives for nuanced issues
- Requires domain knowledge to craft effective rules
- May miss simple issues if users don't run other tools

### Recommendation

Users should run this tool alongside:
- Roslyn analyzers (.NET)
- SonarQube or SonarCloud
- Language-specific linters

## References

- [SQL Caffeine - 10 SQL Anti-Patterns](https://sqlcaffeine.com/)
- [SonarQube Rules](https://rules.sonarsource.com/)
