using CerbiStream.Interfaces;
using CerbiStream.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;

namespace CerbiStream.Classes.OpenTelemetry
{
    public class AppInsightsTelemetryProvider : ITelemetryProvider
    {
        private readonly TelemetryClient _telemetryClient;

        public AppInsightsTelemetryProvider()
        {
            _telemetryClient = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new CerbiTelemetryInitializer());

        }

        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            var enrichedProperties = MergeWithTelemetryContext(properties);
            _telemetryClient.TrackEvent(eventName, enrichedProperties);
        }

        public void TrackException(Exception exception, Dictionary<string, string> properties)
        {
            var enrichedProperties = MergeWithTelemetryContext(properties);
            _telemetryClient.TrackException(exception, enrichedProperties);
        }

        public void TrackDependency(string dependencyType, string target, TimeSpan duration, bool success)
        {
            _telemetryClient.TrackDependency(
                dependencyType,  // dependencyType
                target,          // target
                "UnknownCommand", // commandName (placeholder)
                DateTimeOffset.UtcNow - duration, // startTime
                duration,        // duration
                success          // success
            );
        }

        private static Dictionary<string, string> MergeWithTelemetryContext(Dictionary<string, string> properties)
        {
            var snapshot = TelemetryContext.Snapshot();
            foreach (var kvp in snapshot)
            {
                if (!properties.ContainsKey(kvp.Key) && kvp.Value != null)
                {
                    properties[kvp.Key] = kvp.Value.ToString()!;
                }
            }
            return properties;
        }

    }
}
