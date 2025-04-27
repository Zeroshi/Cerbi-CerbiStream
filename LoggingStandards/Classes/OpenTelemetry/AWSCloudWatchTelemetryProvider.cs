using CerbiStream.Interfaces;
using CerbiStream.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace CerbiStream.Classes.OpenTelemetry
{
    public class AWSCloudWatchTelemetryProvider : ITelemetryProvider
    {
        private readonly IAmazonCloudWatchLogs _client;
        private readonly string _logGroupName = "cerbistream-logs";

        public AWSCloudWatchTelemetryProvider()
        {
            _client = new AmazonCloudWatchLogsClient();
        }

        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            _ = SendLogAsync(SerializeToJson(new
            {
                EventName = eventName,
                Properties = MergeWithTelemetryContext(properties),
                TimestampUtc = DateTime.UtcNow
            }));
        }

        public void TrackException(Exception exception, Dictionary<string, string> properties)
        {
            _ = SendLogAsync(SerializeToJson(new
            {
                ExceptionType = exception.GetType().Name,
                ExceptionMessage = exception.Message,
                StackTrace = exception.StackTrace,
                Properties = MergeWithTelemetryContext(properties),
                TimestampUtc = DateTime.UtcNow
            }));
        }

        public void TrackDependency(string dependencyType, string target, TimeSpan duration, bool success)
        {
            _ = SendLogAsync(SerializeToJson(new
            {
                DependencyType = dependencyType,
                Target = target,
                Success = success,
                DurationMs = duration.TotalMilliseconds,
                TimestampUtc = DateTime.UtcNow
            }));
        }

        private async Task SendLogAsync(string message)
        {
            try
            {
                var request = new PutLogEventsRequest
                {
                    LogGroupName = _logGroupName,
                    LogStreamName = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    LogEvents = new List<InputLogEvent>
                    {
                        new InputLogEvent
                        {
                            Message = message,
                            Timestamp = DateTime.UtcNow
                        }
                    }
                };

                await _client.PutLogEventsAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CerbiStream AWSCloudWatch] Failed to send log: {ex.Message}");
            }
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

        private static string SerializeToJson(object obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }
    }
}
