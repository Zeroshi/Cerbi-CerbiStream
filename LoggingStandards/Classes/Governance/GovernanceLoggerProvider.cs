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

        // Simple pooled dictionary to reduce per-log allocations when converting state
        private static readonly ConcurrentBag<Dictionary<string, object>> s_dictPool = new();

        private static Dictionary<string, object> RentDictionary()
        {
            if (s_dictPool.TryTake(out var d))
            {
                d.Clear();
                return d;
            }
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        private static void ReturnDictionary(Dictionary<string, object> d)
        {
            d.Clear();
            s_dictPool.Add(d);
        }

        public GovernanceLogger(ILogger inner, GovernanceRuntimeAdapter adapter)
            => (_inner, _adapter) = (inner, adapter);

        public IDisposable BeginScope<TState>(TState state) => _inner.BeginScope(state!);
        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is IEnumerable<KeyValuePair<string, object>> kvs)
            {
                // Rent a pooled dictionary to avoid ToDictionary / ToList allocations on hot path
                var dict = RentDictionary();
                try
                {
                    foreach (var kv in kvs)
                    {
                        // Use assignment to preserve last-writer semantics similar to ToDictionary
                        dict[kv.Key] = kv.Value;
                    }

                    _adapter.ValidateAndRedactInPlace(dict);

                    // Pass the dictionary directly as the structured state (Dictionary implements IEnumerable<KeyValuePair<,>>)
                    _inner.Log(logLevel, eventId, (object)dict, exception, (o, e) => formatter((TState)o, e));
                    return;
                }
                finally
                {
                    // Return dictionary to pool after inner logger processed synchronously
                    ReturnDictionary(dict);
                }
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
