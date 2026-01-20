# ADR 0004: Uno Platform for Desktop UI

## Status

Accepted

## Context

The code review tool needs a desktop application for interactive use. Requirements:

- Cross-platform support (Windows, macOS, Linux)
- Modern UI capabilities (data binding, MVVM)
- Shared codebase with CLI via .NET
- Rich diff visualization

## Decision

We will use **Uno Platform** for the desktop application.

### Reasons

1. **Cross-Platform**: Single codebase for Windows, macOS, and Linux desktops
2. **WinUI 3 API**: Modern XAML-based UI with familiar patterns for .NET developers
3. **Code Sharing**: Business logic in shared .NET library works seamlessly
4. **Hot Reload**: Fast development iteration with XAML hot reload
5. **Future Expansion**: Same code can target Web (WASM), iOS, and Android if needed

### Alternatives Considered

1. **WPF**: Windows-only, mature but limited to single platform
2. **Avalonia UI**: Good cross-platform alternative, but smaller ecosystem
3. **.NET MAUI**: Mobile-first, desktop support less mature
4. **Electron + Blazor**: Web technologies, heavier runtime
5. **CLI Only**: Simpler but poor UX for interactive review sessions

## Consequences

### Positive

- Modern, native-feeling UI on all desktop platforms
- Leverage existing WinUI/XAML knowledge
- Strong MVVM support with CommunityToolkit
- Active development and Microsoft backing

### Negative

- Larger binary size than WPF-only
- Some WinUI features may not be available on all platforms
- Build times can be longer for multi-platform targets
- Learning curve for platform-specific considerations

### Implementation Notes

- Use Uno Platform "blank" template with desktop-only targets initially
- Consider adding WASM target later for web-based reviews
- Use CommunityToolkit.Mvvm for MVVM pattern
- Target .NET 10 for latest language features

## References

- [Uno Platform Documentation](https://platform.uno/docs/)
- [Uno Platform GitHub](https://github.com/unoplatform/uno)
