using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CerbiStream.Configuration;
using System.Linq;

public class HealthHostedServiceTests
{
 [Fact]
 public async Task StartAsync_LogsWarning_When_PolicyMissing()
 {
 var mockLogger = new Mock<ILogger<HealthHostedService>>();
 var svc = new HealthHostedService(mockLogger.Object, policyPath: "nonexistent-policy.json");
 await svc.StartAsync(default);

 // Verify that a Log call with LogLevel.Warning was made (inspect invocations to avoid generic signature mismatch)
 var found = mockLogger.Invocations.Any(inv =>
 inv.Method.Name == "Log" &&
 inv.Arguments.Count >0 &&
 inv.Arguments[0] is LogLevel ll &&
 ll == LogLevel.Warning);

 Assert.True(found, "Expected a Log call with LogLevel.Warning but none was found.");
 }
}
