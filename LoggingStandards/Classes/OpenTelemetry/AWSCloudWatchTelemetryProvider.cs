using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using CerbiStream.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CerbiStream.Classes.OpenTelemetry
{
    public class AWSCloudWatchTelemetryProvider : ITelemetryProvider
    {
        private readonly AmazonCloudWatchLogsClient _cloudWatchClient;
        private readonly string _logGroupName = "CerbiStreamLogs";
        private readonly string _logStreamName = $"CerbiStream-{Guid.NewGuid()}";

        public AWSCloudWatchTelemetryProvider()
        {
            _cloudWatchClient = new AmazonCloudWatchLogsClient();
            InitializeLogStream().Wait();
        }

        private async Task InitializeLogStream()
        {
            try
            {
                await _cloudWatchClient.CreateLogGroupAsync(new CreateLogGroupRequest { LogGroupName = _logGroupName });
                await _cloudWatchClient.CreateLogStreamAsync(new CreateLogStreamRequest { LogGroupName = _logGroupName, LogStreamName = _logStreamName });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AWSCloudWatch] Failed to initialize log stream: {ex.Message}");
            }
        }

        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            var message = $"[Event] {eventName} | {string.Join(", ", properties)}";
            SendLog(message);
        }

        public void TrackException(Exception ex, Dictionary<string, string> properties)
        {
            var message = $"[Exception] {ex.Message} | StackTrace: {ex.StackTrace} | {string.Join(", ", properties)}";
            SendLog(message);
        }

        public void TrackDependency(string dependencyName, string command, TimeSpan duration, bool success)
        {
            var message = $"[Dependency] {dependencyName} | Command: {command} | Duration: {duration} | Success: {success}";
            SendLog(message);
        }

        private async void SendLog(string message)
        {
            try
            {
                var logEvent = new InputLogEvent
                {
                    Message = message,
                    Timestamp = DateTime.UtcNow
                };

                await _cloudWatchClient.PutLogEventsAsync(new PutLogEventsRequest
                {
                    LogGroupName = _logGroupName,
                    LogStreamName = _logStreamName,
                    LogEvents = new List<InputLogEvent> { logEvent }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AWSCloudWatch] Failed to send log: {ex.Message}");
            }
        }
    }
}
