using System.Threading;
using CerbiStream.Interfaces;
using System.Collections.Generic;

namespace CerbiStream.Observability
{
 public static class Metrics
 {
 private static long _logsProcessed;
 private static long _redactions;
 private static long _violations;

 // Optional telemetry provider (set during AddCerbiStream registration)
 public static ITelemetryProvider? TelemetryProvider { get; set; }

 public static void Reset()
 {
 Interlocked.Exchange(ref _logsProcessed,0);
 Interlocked.Exchange(ref _redactions,0);
 Interlocked.Exchange(ref _violations,0);
 }

 public static void IncrementLogsProcessed()
 {
 var value = Interlocked.Increment(ref _logsProcessed);
 // Emit lightweight telemetry event if configured
 TryEmitTelemetry("LogsProcessed", value);
 }
 public static void IncrementRedactions(long count =1)
 {
 var value = Interlocked.Add(ref _redactions, count);
 TryEmitTelemetry("Redactions", value);
 }
 public static void IncrementViolations(long count =1)
 {
 var value = Interlocked.Add(ref _violations, count);
 TryEmitTelemetry("Violations", value);
 }

 public static long LogsProcessed => Interlocked.Read(ref _logsProcessed);
 public static long Redactions => Interlocked.Read(ref _redactions);
 public static long Violations => Interlocked.Read(ref _violations);

 private static void TryEmitTelemetry(string metricName, long value)
 {
 try
 {
 if (TelemetryProvider == null) return;
 var props = new Dictionary<string, string>
 {
 ["Metric"] = metricName,
 ["Value"] = value.ToString()
 };
 TelemetryProvider.TrackEvent("CerbiStream.Metric", props);
 }
 catch
 {
 // Safe no-op on telemetry failure
 }
 }
 }
}
