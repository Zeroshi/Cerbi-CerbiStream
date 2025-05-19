using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CerbiStream.Extensions
{
    public static class CerbiLoggerExtensions
    {
        public static CerbiLoggerWrapper Relax(this ILogger logger) => new(logger);

        public static void LogInformation(this ILogger logger, Dictionary<string, object> payload)
        {
            logger.Log(LogLevel.Information, default, payload, null, Format);
        }

        public static void LogWarning(this ILogger logger, Dictionary<string, object> payload)
        {
            logger.Log(LogLevel.Warning, default, payload, null, Format);
        }

        private static string Format(object state, Exception? error) => state?.ToString() ?? string.Empty;
    }

    public class CerbiLoggerWrapper
    {
        private readonly ILogger _logger;

        public CerbiLoggerWrapper(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogTrace(string message) => Log(LogLevel.Trace, message);
        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        public void LogInformation(string message) => Log(LogLevel.Information, message);
        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        public void LogError(string message) => Log(LogLevel.Error, message);
        public void LogCritical(string message) => Log(LogLevel.Critical, message);

        private void Log(LogLevel level, string message)
        {
            var metadata = new Dictionary<string, object>
            {
                ["Message"] = message,
                ["GovernanceRelaxed"] = true,
                ["TimestampUtc"] = DateTime.UtcNow
            };

            _logger.Log(level, default, metadata, null, Format);
        }

        private static string Format(object state, Exception? error)
        {
            return state?.ToString() ?? string.Empty;
        }
    }
}
