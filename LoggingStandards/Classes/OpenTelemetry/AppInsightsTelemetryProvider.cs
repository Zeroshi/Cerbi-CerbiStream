using CerbiStream.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CerbiStream.Classes.OpenTelemetry
{
    public class AppInsightsTelemetryProvider : ITelemetryProvider
    {
        private readonly TelemetryClient _telemetryClient;

        public AppInsightsTelemetryProvider()
        {
            var config = TelemetryConfiguration.Active;
            _telemetryClient = new TelemetryClient(config);
        }

        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            _telemetryClient.TrackEvent(eventName, properties);
        }

        public void TrackException(Exception ex, Dictionary<string, string> properties)
        {
            var telemetry = new ExceptionTelemetry(ex);
            foreach (var prop in properties)
            {
                telemetry.Properties[prop.Key] = prop.Value;
            }
            _telemetryClient.TrackException(telemetry);
        }

        public void TrackDependency(string dependencyName, string command, TimeSpan duration, bool success)
        {
            var dependencyTelemetry = new DependencyTelemetry
            {
                Name = dependencyName,
                Data = command,
                Duration = duration,
                Success = success
            };
            _telemetryClient.TrackDependency(dependencyTelemetry);
        }
    }
}
