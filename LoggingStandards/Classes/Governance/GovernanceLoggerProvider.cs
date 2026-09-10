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
        _adapter.Dispose();
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
            var shouldSendToScoring = ShouldSendToScoring;
            var scoringMessage = shouldSendToScoring ? formatter(state, exception) : null;

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

                // Send detailed evidence only when legacy mode is active or the event needs investigation.
                if (ShouldSendDetailedScoring(dict))
                    SendToScoringQueue(logLevel, scoringMessage!, dict, exception);
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

                // Preserve detailed scoring for non-dictionary states because the provider cannot prove pass/violation status here.
                if (ShouldSendDetailedScoring(null))
                    SendToScoringQueue(logLevel, scoringMessage!, null, exception);
            }
            catch
            {
                _inner.Log(logLevel, eventId, state!, exception, formatter);
                if (ShouldSendDetailedScoring(null))
                    SendToScoringQueue(logLevel, scoringMessage!, null, exception);
            }
        }

        private bool ShouldSendToScoring
            => _ScoringService != null && _options != null && !_options.DisableQueueSending;

        private bool SummaryModeEnabled
            => !string.IsNullOrWhiteSpace(_options?.GovernanceSummaryEndpoint)
               && !string.IsNullOrWhiteSpace(_options.GovernanceSummaryTenantId ?? _options.TenantId);

        private bool ShouldSendDetailedScoring(Dictionary<string, object>? data)
        {
            if (!ShouldSendToScoring)
                return false;
            if (!SummaryModeEnabled)
                return true;
            return data is null || !IsHealthyPass(data);
        }

        private static bool IsHealthyPass(Dictionary<string, object> data)
        {
            if (TryGetBool(data, "GovernanceRelaxed"))
                return false;

            var decision = TryGetString(data, "GovernanceDecision");
            var action = TryGetString(data, "EnforcementAction");
            var mode = TryGetString(data, "GovernanceMode");

            if (!string.IsNullOrWhiteSpace(decision)
                && !decision.Equals("allowed", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(action)
                && !action.Equals("none", StringComparison.OrdinalIgnoreCase)
                && !action.Equals("allow", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(mode)
                && (mode.Equals("unknown", StringComparison.OrdinalIgnoreCase)
                    || mode.Equals("invalid", StringComparison.OrdinalIgnoreCase)
                    || mode.Equals("relax", StringComparison.OrdinalIgnoreCase)
                    || mode.Equals("relaxed", StringComparison.OrdinalIgnoreCase)))
                return false;

            return !HasAnyViolation(data.TryGetValue("GovernanceViolations", out var rawViolations) ? rawViolations : null)
                   && !HasAnyViolation(data.TryGetValue("GovernanceViolationsStructured", out var rawStructured) ? rawStructured : null);
        }

        private static string? TryGetString(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        private static bool TryGetBool(Dictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out var value))
                return false;
            if (value is bool b)
                return b;
            return bool.TryParse(value?.ToString(), out var parsed) && parsed;
        }

        private static bool HasAnyViolation(object? raw)
        {
            if (raw is null)
                return false;
            if (raw is string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;
                try
                {
                    using var doc = JsonDocument.Parse(text);
                    return doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0;
                }
                catch
                {
                    return false;
                }
            }
            if (raw is JsonElement element)
                return element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0;
            if (raw is System.Collections.IEnumerable enumerable)
            {
                foreach (var _ in enumerable)
                    return true;
            }
            return false;
        }

        private void SendToScoringQueue(LogLevel logLevel, string message, Dictionary<string, object>? data, Exception? exception)
        {
            if (!ShouldSendToScoring)
                return;

            try
            {
                var logEntry = data ?? new Dictionary<string, object>();
                logEntry["LogLevel"] = logLevel.ToString();
                logEntry["Message"] = message;
                if (exception != null)
                    logEntry["Exception"] = exception.ToString();

                var logId = Guid.NewGuid().ToString("N");
                var scoringEvent = ScoringEventTransformer.Transform(logEntry, logId, _options!);
                _ScoringService!.Enqueue(scoringEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CerbiStream] Failed to send to scoring queue: {ex.Message}");
            }
        }
    }
}
