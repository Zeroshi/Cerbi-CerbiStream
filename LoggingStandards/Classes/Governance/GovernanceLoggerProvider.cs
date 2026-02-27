using CerbiShield.Contracts.Scoring;
using CerbiStream.Configuration;
using CerbiStream.Scoring;
using CerbiStream.Services;
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
    private readonly CerbiStreamOptions? _options;
    private readonly IScoringService? _ScoringService;

    public GovernanceLoggerProvider(ILoggerFactory innerFactory, GovernanceRuntimeAdapter adapter)
        : this(innerFactory, adapter, null, null) { }

    public GovernanceLoggerProvider(ILoggerFactory innerFactory, GovernanceRuntimeAdapter adapter, CerbiStreamOptions? options, IScoringService? ScoringService)
    {
        _innerFactory = innerFactory;
        _adapter = adapter;
        _options = options;
        _ScoringService = ScoringService;

        if (_ScoringService != null)
        {
            Console.WriteLine("[CerbiStream] GovernanceLoggerProvider initialized with ScoringService");
        }
    }

    public ILogger CreateLogger(string categoryName)
        => new GovernanceLogger(_innerFactory.CreateLogger(categoryName), _adapter, _options, _ScoringService);

    public void Dispose() 
    {
        _ScoringService?.Dispose();
    }

    private sealed class GovernanceLogger : ILogger
    {
        private readonly ILogger _inner;
        private readonly GovernanceRuntimeAdapter _adapter;
        private readonly CerbiStreamOptions? _options;
        private readonly IScoringService? _ScoringService;

        public GovernanceLogger(ILogger inner, GovernanceRuntimeAdapter adapter, CerbiStreamOptions? options, IScoringService? ScoringService)
        {
            _inner = inner;
            _adapter = adapter;
            _options = options;
            _ScoringService = ScoringService;
        }

        public IDisposable BeginScope<TState>(TState state) => _inner.BeginScope(state!);
        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

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

                // Send to scoring queue
                SendToScoringQueue(logLevel, message, dict, exception);
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

                // Send to scoring queue
                SendToScoringQueue(logLevel, message, null, exception);
            }
            catch
            {
                _inner.Log(logLevel, eventId, state!, exception, formatter);
                SendToScoringQueue(logLevel, message, null, exception);
            }
        }

        private void SendToScoringQueue(LogLevel logLevel, string message, Dictionary<string, object>? data, Exception? exception)
        {
            if (_ScoringService == null || _options == null || _options.DisableQueueSending)
                return;

            try
            {
                var logEntry = data ?? new Dictionary<string, object>();
                logEntry["LogLevel"] = logLevel.ToString();
                logEntry["Message"] = message;
                if (exception != null)
                    logEntry["Exception"] = exception.ToString();

                var logId = Guid.NewGuid().ToString("N");
                var scoringEvent = ScoringEventTransformer.Transform(logEntry, logId, _options);
                _ScoringService.Enqueue(scoringEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CerbiStream] Failed to send to scoring queue: {ex.Message}");
            }
        }
    }
}
