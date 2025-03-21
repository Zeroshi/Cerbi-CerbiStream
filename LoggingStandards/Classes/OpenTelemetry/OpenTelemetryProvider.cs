using CerbiStream.Interfaces;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry.Resources;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;


namespace CerbiStream.Classes.OpenTelemetry
{
    public class OpenTelemetryProvider : ITelemetryProvider
    {
        private readonly TracerProvider _tracerProvider;
        private readonly ActivitySource _activitySource;

        public OpenTelemetryProvider()
        {
            _tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CerbiStream"))
                .AddSource("CerbiStream")
                .AddConsoleExporter()  // ✅ Optional: Console logging for debugging
                .Build();

            _activitySource = new ActivitySource("CerbiStream");
        }

        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            using (var activity = _activitySource.StartActivity(eventName))
            {
                if (activity == null) return;

                foreach (var prop in properties)
                {
                    activity.SetTag(prop.Key, prop.Value);
                }
            }
        }

        public void TrackException(Exception ex, Dictionary<string, string> properties)
        {
            using (var activity = _activitySource.StartActivity("Exception"))
            {
                if (activity == null) return;

                activity.SetTag("ExceptionMessage", ex.Message);
                activity.SetTag("StackTrace", ex.StackTrace);
                foreach (var prop in properties)
                {
                    activity.SetTag(prop.Key, prop.Value);
                }
            }
        }

        public void TrackDependency(string dependencyName, string command, TimeSpan duration, bool success)
        {
            using (var activity = _activitySource.StartActivity(dependencyName))
            {
                if (activity == null) return;

                activity.SetTag("DependencyCommand", command);
                activity.SetTag("Duration", duration.ToString());
                activity.SetTag("Success", success.ToString());
            }
        }
    }

}
