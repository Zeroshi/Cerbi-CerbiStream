using Xunit;
using CerbiStream.Observability;

namespace CerbiStream.Tests
{
 public class MetricsTests
 {
 [Fact(DisplayName = "Metrics - counters increment and reset")]
 public void Metrics_IncrementAndReset()
 {
 // Capture baseline to avoid flakiness from parallel tests
 var beforeLogs = Metrics.LogsProcessed;
 var beforeRedactions = Metrics.Redactions;
 var beforeViolations = Metrics.Violations;

 Metrics.IncrementLogsProcessed();
 Metrics.IncrementRedactions(2);
 Metrics.IncrementViolations(3);

 Assert.Equal(beforeLogs +1, Metrics.LogsProcessed);
 Assert.Equal(beforeRedactions +2, Metrics.Redactions);
 Assert.Equal(beforeViolations +3, Metrics.Violations);

 // Reset and verify cleared
 Metrics.Reset();
 Assert.Equal(0, Metrics.LogsProcessed);
 Assert.Equal(0, Metrics.Redactions);
 Assert.Equal(0, Metrics.Violations);
 }
 }
}
