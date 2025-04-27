using CerbiStream.Interfaces;
using System;

namespace CerbiStream.Classes.OpenTelemetry
{
    public static class TelemetryProviderFactory
    {
        public static ITelemetryProvider CreateTelemetryProvider(string providerName)
        {
            return providerName.ToLower() switch
            {
                "appinsights" => new AppInsightsTelemetryProvider(),
                "datadog" => new DatadogTelemetryProvider(),
                "opentelemetry" => new OpenTelemetryProvider(),
                "awscloudwatch" => new AWSCloudWatchTelemetryProvider(),
                "gcpstackdriver" => new GCPStackdriverTelemetryProvider(),
                "optimizedopentelemetry" => new OptimizedTelemetryProvider(),
                _ => throw new ArgumentException($"Unknown telemetry provider: {providerName}")
            };

        }
    }
}
