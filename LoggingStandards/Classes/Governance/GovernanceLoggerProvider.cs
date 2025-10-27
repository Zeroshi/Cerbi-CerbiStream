using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Collections.Concurrent;

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
                // Create a fresh dictionary to hold structured state we will validate/redact.
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in kvs)
                {
                    // Preserve last-writer semantics similar to ToDictionary on duplicate keys
                    dict[kv.Key] = kv.Value;
                }

                _adapter.ValidateAndRedactInPlace(dict);

                // Pass redacted dictionary as structured state. Keep original formatter output by ignoring 'o'
                _inner.Log(logLevel, eventId, (object)dict, exception, (_, e) => formatter(state, e));
                return;
            }

            // Fallback: try JSON mapping
            try
            {
                var json = JsonSerializer.Serialize(state!);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement.Clone();
                _adapter.ValidateAndRedactInPlace(root);

                _inner.Log(logLevel, eventId, (object)root, exception, (_, e) => formatter(state, e));
            }
            catch
            {
                _inner.Log(logLevel, eventId, state!, exception, formatter);
            }
        }
    }
}
