using System.Threading;

namespace CerbiStream.Observability
{
 public static class Metrics
 {
 private static long _logsProcessed;
 private static long _redactions;
 private static long _violations;

 public static void Reset()
 {
 Interlocked.Exchange(ref _logsProcessed,0);
 Interlocked.Exchange(ref _redactions,0);
 Interlocked.Exchange(ref _violations,0);
 }

 public static void IncrementLogsProcessed() => Interlocked.Increment(ref _logsProcessed);
 public static void IncrementRedactions(long count =1) => Interlocked.Add(ref _redactions, count);
 public static void IncrementViolations(long count =1) => Interlocked.Add(ref _violations, count);

 public static long LogsProcessed => Interlocked.Read(ref _logsProcessed);
 public static long Redactions => Interlocked.Read(ref _redactions);
 public static long Violations => Interlocked.Read(ref _violations);
 }
}
