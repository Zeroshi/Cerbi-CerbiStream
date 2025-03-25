using Microsoft.Extensions.Logging;
using CerbiStream.Telemetry;

namespace CerbiStream
{
    public static class Cerbi
    {
        public static ILoggingBuilder AddDevLogging(this ILoggingBuilder builder)
        {
            return builder.AddCerbiStream(options =>
            {
                options.EnableDevMode();
                TelemetryContext.ServiceName = "DevApp";
                TelemetryContext.OriginApp = "Local";
                TelemetryContext.UserType = "Developer";
            });
        }
    }
}
