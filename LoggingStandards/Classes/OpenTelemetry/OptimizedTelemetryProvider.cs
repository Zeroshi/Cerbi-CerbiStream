using CerbiStream.Interfaces;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry.Exporter;

namespace CerbiStream.Classes.OpenTelemetry
{
    public class OptimizedTelemetryProvider : ITelemetryProvider
    {
        private readonly TracerProvider _tracerProvider;
        private readonly ActivitySource _activitySource;
        private readonly bool _isEnabled;
        private readonly double _samplingRate;
        private readonly HashSet<string> _excludedEvents; // ✅ Excluded events

        public OptimizedTelemetryProvider(bool enableTelemetry = true, double samplingRate = 1.0)
        {
            _isEnabled = enableTelemetry;
            _samplingRate = Math.Clamp(samplingRate, 0, 1);

            _activitySource = new ActivitySource("CerbiStream");

            // ✅ Define events that should NOT be tracked
            _excludedEvents = new HashSet<string>
            {
                "DebugLog",
                "HealthCheck",
                "BackgroundJobExecution"
            };

            _tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CerbiStream"))
                .AddSource("CerbiStream")
                .AddConsoleExporter()
                .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(_samplingRate)))
                .Build();
        }

        // ✅ Track Event
        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            if (_excludedEvents.Contains(eventName)) return;

            using (var activity = _activitySource.StartActivity(eventName, ActivityKind.Internal))
            {
                if (activity != null)
                {
                    foreach (var prop in properties)
                    {
                        activity.SetTag(prop.Key, prop.Value);
                    }
                }
            }
        }

        // ✅ Track Exception (Fix for CS0535)
        public void TrackException(Exception ex, Dictionary<string, string> properties)
        {
            using (var activity = _activitySource.StartActivity("Exception", ActivityKind.Internal))
            {
                if (activity != null)
                {
                    activity.SetTag("ExceptionMessage", ex.Message);
                    activity.SetTag("StackTrace", ex.StackTrace);

                    foreach (var prop in properties)
                    {
                        activity.SetTag(prop.Key, prop.Value);
                    }
                }
            }
        }

        // ✅ Track Dependency (Fix for CS0535)
        public void TrackDependency(string dependencyName, string command, TimeSpan duration, bool success)
        {
            using (var activity = _activitySource.StartActivity(dependencyName, ActivityKind.Client))
            {
                if (activity != null)
                {
                    activity.SetTag("DependencyCommand", command);
                    activity.SetTag("Duration", duration.ToString());
                    activity.SetTag("Success", success.ToString());
                }
            }
        }
    }
}
