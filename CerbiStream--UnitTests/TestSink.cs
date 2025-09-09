using Microsoft.Extensions.Logging;

namespace CerbiStream.GovernanceRuntime.Tests;

internal sealed class TestSink : ILoggerProvider, ILogger
{
    public readonly List<(LogLevel Level, object? State)> Events = new();
    public ILogger CreateLogger(string categoryName) => this;
    public void Dispose() { }
    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId id, TState state, Exception? ex, Func<TState, Exception?, string> fmt)
        => Events.Add((logLevel, state));
    private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
}
