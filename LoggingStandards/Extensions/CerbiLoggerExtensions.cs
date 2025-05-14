using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CerbiStream.Extensions
{
    public static class CerbiLoggerExtensions
    {
        public static CerbiLoggerWrapper Relax(this ILogger logger) => new(logger, true);

        public static void LogInformation(this ILogger logger, object payload)
            => logger.Log(LogLevel.Information, default, payload, null, Format);

        public static void LogWarning(this ILogger logger, object payload)
            => logger.Log(LogLevel.Warning, default, payload, null, Format);

        private static string Format(object state, Exception? error) => state?.ToString() ?? string.Empty;
    }

    public class CerbiLoggerWrapper
    {
        private readonly ILogger _logger;

        public CerbiLoggerWrapper(ILogger logger, bool relaxed)
        {
            _logger = logger;
        }

        public void LogWarning(object payload)
        {
            _logger.Log(LogLevel.Warning, default, MarkRelaxed(payload), null, Format);
        }

        public void LogInformation(object payload)
        {
            _logger.Log(LogLevel.Information, default, MarkRelaxed(payload), null, Format);
        }

        private static object MarkRelaxed(object payload)
        {
            if (payload is Dictionary<string, object> dict)
            {
                dict["GovernanceRelaxed"] = true;
                return dict;
            }

            return new
            {
                GovernanceRelaxed = true,
                Payload = payload
            };
        }

        private static string Format(object state, Exception? error) => state?.ToString() ?? string.Empty;
    }
}
