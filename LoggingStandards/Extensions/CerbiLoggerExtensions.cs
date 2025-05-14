using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CerbiStream.Extensions
{
    public static class CerbiLoggerExtensions
    {
        public static void LogInformation(this ILogger logger, object payload)
        {
            logger.Log(LogLevel.Information, new EventId(), payload, null, (s, e) => s?.ToString() ?? string.Empty);
        }

        public static void LogWarning(this ILogger logger, object payload)
        {
            logger.Log(LogLevel.Warning, new EventId(), payload, null, (s, e) => s?.ToString() ?? string.Empty);
        }

        public static CerbiLoggerWrapper Relax(this ILogger logger)
        {
            return new CerbiLoggerWrapper(logger, relaxed: true);
        }
    }

    public class CerbiLoggerWrapper
    {
        private readonly ILogger _logger;
        private readonly bool _relaxed;

        public CerbiLoggerWrapper(ILogger logger, bool relaxed)
        {
            _logger = logger;
            _relaxed = relaxed;
        }

        public void LogWarning(object payload)
        {
            if (payload is Dictionary<string, object> dict)
            {
                dict["GovernanceRelaxed"] = true;
            }

            _logger.Log(LogLevel.Warning, new EventId(), payload, null, (s, e) => s?.ToString() ?? string.Empty);
        }
    }
}
