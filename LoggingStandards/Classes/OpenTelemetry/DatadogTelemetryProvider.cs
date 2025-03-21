using CerbiStream.Interfaces;
using Datadog.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CerbiStream.Classes.OpenTelemetry
{
    public class DatadogTelemetryProvider : ITelemetryProvider
    {
        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            using (var scope = Tracer.Instance.StartActive(eventName))
            {
                foreach (var prop in properties)
                {
                    scope.Span.SetTag(prop.Key, prop.Value);
                }
            }
        }

        public void TrackException(Exception ex, Dictionary<string, string> properties)
        {
            using (var scope = Tracer.Instance.StartActive("Exception"))
            {
                scope.Span.SetTag("ExceptionMessage", ex.Message);
                scope.Span.SetTag("StackTrace", ex.StackTrace);
                foreach (var prop in properties)
                {
                    scope.Span.SetTag(prop.Key, prop.Value);
                }
            }
        }

        public void TrackDependency(string dependencyName, string command, TimeSpan duration, bool success)
        {
            using (var scope = Tracer.Instance.StartActive(dependencyName))
            {
                scope.Span.SetTag("DependencyCommand", command);
                scope.Span.SetTag("Duration", duration.ToString());
                scope.Span.SetTag("Success", success.ToString());
            }
        }
    }
}
