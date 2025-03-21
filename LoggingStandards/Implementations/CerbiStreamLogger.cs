using CerbiStream.Configuration;
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
            if (_options.DevModeEnabled)
            {
                Console.WriteLine($"[DevMode] {logLevel}: {formatter(state, exception)}");
                return;
            }

            // TODO: Implement actual logic to send log to queues
            Console.WriteLine($"[{_options.QueueType}] {logLevel}: {formatter(state, exception)}");
        }

        public void LogEvent(string message, LogLevel level, Dictionary<string, string> metadata)
        {
            Console.WriteLine($"{level}: {message}"); // Standard Console Log

            if (_options.AlsoSendToTelemetry && _options.TelemetryProvider != null)
            {
                _options.TelemetryProvider.TrackEvent("LogEvent", metadata);
            }
        }


    }
}
