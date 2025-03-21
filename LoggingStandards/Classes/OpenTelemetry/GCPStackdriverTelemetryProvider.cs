using CerbiStream.Interfaces;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;

namespace CerbiStream.Classes.OpenTelemetry
{
    public class GCPStackdriverTelemetryProvider : ITelemetryProvider
    {
        private readonly LoggingServiceV2Client _loggingClient;
        private readonly string _logName = "CerbiStreamLogs";

        public GCPStackdriverTelemetryProvider()
        {
            _loggingClient = LoggingServiceV2Client.Create();
        }

        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            LogEntry logEntry = CreateLogEntry($"[Event] {eventName} | {string.Join(", ", properties)}", LogSeverity.Info);
            SendLog(logEntry);
        }

        public void TrackException(Exception ex, Dictionary<string, string> properties)
        {
            LogEntry logEntry = CreateLogEntry($"[Exception] {ex.Message} | StackTrace: {ex.StackTrace} | {string.Join(", ", properties)}", LogSeverity.Error);
            SendLog(logEntry);
        }

        public void TrackDependency(string dependencyName, string command, TimeSpan duration, bool success)
        {
            LogEntry logEntry = CreateLogEntry($"[Dependency] {dependencyName} | Command: {command} | Duration: {duration} | Success: {success}", LogSeverity.Debug);
            SendLog(logEntry);
        }

        private LogEntry CreateLogEntry(string message, LogSeverity severity)
        {
            return new LogEntry
            {
                LogName = _logName,
                Severity = severity,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                TextPayload = message
            };
        }

        private void SendLog(LogEntry logEntry)
        {
            try
            {
                _loggingClient.WriteLogEntries(new LogName("your-gcp-project-id", _logName), null, null, new[] { logEntry });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GCP Stackdriver] Failed to send log: {ex.Message}");
            }
        }
    }
}
