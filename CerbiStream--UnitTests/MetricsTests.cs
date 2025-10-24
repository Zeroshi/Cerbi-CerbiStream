using Xunit;
using CerbiStream.Observability;

namespace CerbiStream.Tests
{
 public class MetricsTests
 {
 [Fact(DisplayName = "Metrics - counters increment and reset")]
 public void Metrics_IncrementAndReset()
 {
 Metrics.Reset();
 Metrics.IncrementLogsProcessed();
 Metrics.IncrementRedactions(2);
 Metrics.IncrementViolations(3);

 Assert.Equal(1, Metrics.LogsProcessed);
 Assert.Equal(2, Metrics.Redactions);
 Assert.Equal(3, Metrics.Violations);

 Metrics.Reset();
 Assert.Equal(0, Metrics.LogsProcessed);
 Assert.Equal(0, Metrics.Redactions);
 Assert.Equal(0, Metrics.Violations);
 }
 }
}
