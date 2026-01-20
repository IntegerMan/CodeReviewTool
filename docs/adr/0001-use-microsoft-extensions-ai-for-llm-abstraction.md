# ADR 0001: Use Microsoft.Extensions.AI for LLM Abstraction

## Status

Accepted

## Context

The code review tool needs to interact with Large Language Models (LLMs) to analyze code diffs. Users may want to use different AI providers based on:

- Enterprise requirements (Azure OpenAI)
- Cost considerations (OpenAI direct API)
- Privacy/offline requirements (Ollama local models)

We need an abstraction layer that allows swapping providers without changing business logic.

## Decision

We will use `Microsoft.Extensions.AI` and its `IChatClient` interface as the abstraction layer for AI providers.

### Reasons

1. **Official Microsoft Library**: Part of the .NET Extensions ecosystem, ensuring long-term support and compatibility with other Microsoft libraries
2. **Unified Interface**: `IChatClient` provides a consistent API across providers
3. **Dependency Injection Support**: Built-in DI extensions via `AddChatClient()`
4. **Provider Ecosystem**: Official adapters for:
   - `Microsoft.Extensions.AI.OpenAI` (OpenAI and Azure OpenAI)
   - `Microsoft.Extensions.AI.Ollama` (local Ollama models)
5. **Middleware Pipeline**: Supports caching, logging, and telemetry via decorators
6. **Future-Proof**: As the Microsoft-recommended approach, new providers will likely support this interface

### Alternatives Considered

1. **Agent Framework**: More feature-rich but heavier; better for agentic scenarios. We may migrate if we add multi-step reasoning.
2. **Direct SDK Usage**: Each provider has its own SDK, but this creates tight coupling.
3. **Custom Abstraction**: More control but significant maintenance burden.

## Consequences

### Positive

- Easy provider switching via configuration
- Testable code through `IChatClient` mocking
- Consistent error handling across providers
- Access to Microsoft's ongoing improvements

### Negative

- Some provider packages are still in preview (OpenAI, Ollama adapters)
- Limited to capabilities exposed by `IChatClient` interface
- May need to upgrade packages frequently during preview period

### Risks

- Preview packages may have breaking changes before GA
- Some advanced provider features may not be exposed through the abstraction

## References

- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
- [.NET AI Samples](https://github.com/dotnet/ai-samples)
