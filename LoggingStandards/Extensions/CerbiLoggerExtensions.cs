using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CerbiStream.Extensions
{
    /// <summary>
    /// Provides governance-aware extensions for structured logging.
    /// </summary>
    public static class CerbiLoggerExtensions
    {
        /// <summary>
        /// Flags the logger to bypass governance checks (e.g., for raw JSON or non-conforming structures).
        /// </summary>
        public static CerbiLoggerWrapper Relax(this ILogger logger) => new(logger);
    }

    /// <summary>
    /// Wrapper for relaxed log calls. Ensures governance violations are bypassed by tagging logs.
    /// </summary>
    public class CerbiLoggerWrapper
    {
        private readonly ILogger _logger;

        public CerbiLoggerWrapper(ILogger logger)
        {
            _logger = logger;
        }

        public void LogInformation(object payload)
        {
            _logger.Log(LogLevel.Information, default, MarkRelaxed(payload), null, Format);
        }

        public void LogWarning(object payload)
        {
            _logger.Log(LogLevel.Warning, default, MarkRelaxed(payload), null, Format);
        }

        public void LogError(object payload)
        {
            _logger.Log(LogLevel.Error, default, MarkRelaxed(payload), null, Format);
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
