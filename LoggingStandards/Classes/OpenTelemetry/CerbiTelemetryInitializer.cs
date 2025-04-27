using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using CerbiStream.Telemetry;

namespace CerbiStream.Classes.OpenTelemetry
{
    public class CerbiTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            var snapshot = TelemetryContext.Snapshot();

            foreach (var kvp in snapshot)
            {
                // Safely add to existing telemetry properties
                if (!telemetry.Context.GlobalProperties.ContainsKey(kvp.Key) && kvp.Value != null)
                {
                    telemetry.Context.GlobalProperties[kvp.Key] = kvp.Value.ToString()!;
                }
            }
        }
    }
}
