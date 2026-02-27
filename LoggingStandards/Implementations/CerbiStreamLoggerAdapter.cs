using CerbiShield.Contracts.Scoring;
using CerbiStream.Classes.OpenTelemetry;
using CerbiStream.Configuration;
using CerbiStream.Scoring;
using CerbiStream.Services;
using CerbiStream.Telemetry;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CerbiStream.Logging.Configuration
{
    public class CerbiStreamLoggerAdapter : ILogger
    {
        private readonly string _categoryName;
        private readonly CerbiStreamOptions _options;
        private readonly IScoringService? _ScoringService;

        public CerbiStreamLoggerAdapter(string categoryName, CerbiStreamOptions options, IScoringService? ScoringService = null)
        {
            _categoryName = categoryName;
            _options = options;
            _ScoringService = ScoringService;
        }

        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            // Always output to console in dev mode
            if (_options.EnableConsoleOutput)
            {
                Console.WriteLine($"[Dev] {logLevel}: {message}");
            }
            else
            {
                Console.WriteLine($"[{_options.QueueType}] {logLevel}: {message}");
            }

            // Send to scoring queue if enabled
            if (_ScoringService != null && !_options.DisableQueueSending)
            {
                var logEntry = new Dictionary<string, object?>
                {
                    ["Category"] = _categoryName,
                    ["LogLevel"] = logLevel.ToString(),
                    ["Message"] = message,
                    ["Exception"] = exception?.ToString()
                };
                var logId = Guid.NewGuid().ToString("N");
                var scoringEvent = ScoringEventTransformer.Transform(logEntry, logId, _options);
                _ScoringService.Enqueue(scoringEvent);
            }
        }

        public void LogEvent(string message, LogLevel level, Dictionary<string, string> metadata)
        {
            Console.WriteLine($"{level}: {message}");

            // ? Centralized enrichment
            EnrichWithTelemetryContext(metadata);

            if (_options.AlsoSendToTelemetry && _options.TelemetryProvider != null)
            {
                _options.TelemetryProvider.TrackEvent("LogEvent", metadata);
            }
        }

        // ? Private helper for enriching metadata from TelemetryContext
        private static void EnrichWithTelemetryContext(Dictionary<string, string> metadata)
        {
            foreach (var kvp in TelemetryContext.Snapshot())
            {
                if (!metadata.ContainsKey(kvp.Key))
                    metadata[kvp.Key] = kvp.Value?.ToString() ?? "null";
            }
        }
    }
}
