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
            _logger = logger;
        }

        public void LogWarning(Dictionary<string, object> payload)
        {
            payload["GovernanceRelaxed"] = true;
            _logger.Log(LogLevel.Warning, default, payload, null, Format);
        }

        public void LogInformation(Dictionary<string, object> payload)
        {
            payload["GovernanceRelaxed"] = true;
            _logger.Log(LogLevel.Information, default, payload, null, Format);
        }

        private static string Format(object state, Exception? error) => state?.ToString() ?? string.Empty;
    }
}
