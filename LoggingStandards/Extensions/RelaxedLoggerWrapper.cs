using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Cerbi.Governance
{
    public class RelaxedLoggerWrapper : ILogger
    {
        private readonly ILogger _inner;

        public RelaxedLoggerWrapper(ILogger inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _inner.BeginScope(state!);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _inner.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // If state is structured, inject "GovernanceRelaxed"
            if (state is IReadOnlyList<KeyValuePair<string, object?>> structured)
            {
                var extended = new List<KeyValuePair<string, object?>>(structured)
                {
                    new KeyValuePair<string, object?>("GovernanceRelaxed", true)
                };

                _inner.Log(logLevel, eventId, (TState)(object)extended, exception, formatter);
            }
            else
            {
                // If not structured, pass through with additional scope
                using (_inner.BeginScope(new Dictionary<string, object>
                {
                    ["GovernanceRelaxed"] = true
                }))
                {
                    _inner.Log(logLevel, eventId, state!, exception, formatter);
                }
            }
        }
    }

    public static class LoggerRelaxExtension
    {
        public static ILogger RelaxGovernance(this ILogger logger)
        {
            return new RelaxedLoggerWrapper(logger);
        }
    }
}
