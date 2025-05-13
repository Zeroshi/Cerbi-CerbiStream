using Microsoft.Extensions.Logging;
using CerbiStream.Telemetry;
using CerbiStream.Configuration;

namespace CerbiStream
{
    public static class Cerbi
    {
        public static ILoggingBuilder AddDevLogging(this ILoggingBuilder builder)
        {
            return builder.AddCerbiStream(options =>
            {
                options.EnableDeveloperModeWithoutTelemetry();
                TelemetryContext.ServiceName = "DevApp";
                TelemetryContext.OriginApp = "Local";
                TelemetryContext.UserType = "Developer";
            });
        }
    }
}
