using CerbiStream.Classes.OpenTelemetry;
using CerbiStream.Configuration;
using CerbiStream.Telemetry;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CerbiStream.Logging.Configuration
{
    public class CerbiStreamLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly CerbiStreamOptions _options;

        public CerbiStreamLogger(string categoryName, CerbiStreamOptions options)
        {
            _categoryName = categoryName;
            _options = options;
        }

        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_options.EnableConsoleOutput)
            {
                Console.WriteLine($"[Dev] {logLevel}: {formatter(state, exception)}");
                return;
            }

            Console.WriteLine($"[{_options.QueueType}] {logLevel}: {formatter(state, exception)}");
        }

        public void LogEvent(string message, LogLevel level, Dictionary<string, string> metadata)
        {
            Console.WriteLine($"{level}: {message}");

            // ✅ Centralized enrichment
            EnrichWithTelemetryContext(metadata);

            if (_options.AlsoSendToTelemetry && _options.TelemetryProvider != null)
            {
                _options.TelemetryProvider.TrackEvent("LogEvent", metadata);
            }
        }

        // ✅ Private helper for enriching metadata from TelemetryContext
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
