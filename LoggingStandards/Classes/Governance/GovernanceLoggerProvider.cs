using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CerbiStream.GovernanceRuntime.Governance;

public sealed class GovernanceLoggerProvider : ILoggerProvider
{
    private readonly ILoggerFactory _innerFactory;
    private readonly GovernanceRuntimeAdapter _adapter;

    public GovernanceLoggerProvider(ILoggerFactory innerFactory, GovernanceRuntimeAdapter adapter)
        => (_innerFactory, _adapter) = (innerFactory, adapter);

    public ILogger CreateLogger(string categoryName)
        => new GovernanceLogger(_innerFactory.CreateLogger(categoryName), _adapter);

    public void Dispose() { }

    private sealed class GovernanceLogger : ILogger
    {
        private readonly ILogger _inner;
        private readonly GovernanceRuntimeAdapter _adapter;

        public GovernanceLogger(ILogger inner, GovernanceRuntimeAdapter adapter)
            => (_inner, _adapter) = (inner, adapter);

        public IDisposable BeginScope<TState>(TState state) => _inner.BeginScope(state!);
        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is IEnumerable<KeyValuePair<string, object>> kvs)
            {
                var dict = kvs.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
                _adapter.ValidateAndRedactInPlace(dict);
                var enriched = dict.ToList(); // rehydrate as structured state
                _inner.Log(logLevel, eventId, (object)enriched, exception, (o, e) => formatter((TState)o, e));
                return;
            }

            // Fallback: try JSON mapping
            try
            {
                var json = JsonSerializer.Serialize(state!);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement.Clone();
                _adapter.ValidateAndRedactInPlace(root);

                _inner.Log(logLevel, eventId, (object)root, exception, (o, e) => formatter((TState)o, e));
            }
            catch
            {
                _inner.Log(logLevel, eventId, state!, exception, formatter);
            }
        }
    }
}
