using CerbiStream.Logging.Configuration;
using CerbiStream.Interfaces;
using CerbiStream.Observability;

namespace CerbiStream.IntegrationTests.Tests;

/// <summary>
/// Tests for telemetry and observability integration
/// </summary>
public static class TelemetryTests
{
    public static async Task RunAllAsync(TestRunner runner)
    {
        Console.WriteLine("\n📊 Telemetry & Observability Tests");
        Console.WriteLine("   Testing metrics and telemetry integration...\n");

        runner.RunTest("Metrics counters work correctly", () =>
        {
            Metrics.Reset();
            
            Assert(Metrics.LogsProcessed == 0, "LogsProcessed should start at 0");
            Assert(Metrics.Redactions == 0, "Redactions should start at 0");
            Assert(Metrics.Violations == 0, "Violations should start at 0");
            
            Metrics.IncrementLogsProcessed();
            Metrics.IncrementRedactions(5);
            Metrics.IncrementViolations(3);
            
            Assert(Metrics.LogsProcessed == 1, "LogsProcessed should be 1");
            Assert(Metrics.Redactions == 5, "Redactions should be 5");
            Assert(Metrics.Violations == 3, "Violations should be 3");
            
            Metrics.Reset();
            Assert(Metrics.LogsProcessed == 0, "LogsProcessed should be reset");
        });

        runner.RunTest("Custom telemetry provider can be registered", () =>
        {
            var mockProvider = new MockTelemetryProvider();
            
            var options = new CerbiStreamOptions()
                .EnableDeveloperMode()
                .WithTelemetryProvider(mockProvider);
            
            Assert(options.TelemetryProvider == mockProvider, "Provider should be registered");
        });

        runner.RunTest("TelemetryContext captures values", () =>
        {
            CerbiStream.Telemetry.TelemetryContext.Clear();
            
            CerbiStream.Telemetry.TelemetryContext.ServiceName = "TestService";
            CerbiStream.Telemetry.TelemetryContext.Feature = "Checkout";
            CerbiStream.Telemetry.TelemetryContext.UserType = "Premium";
            
            var snapshot = CerbiStream.Telemetry.TelemetryContext.Snapshot();
            
            Assert(snapshot["ServiceName"]?.ToString() == "TestService", "ServiceName should be captured");
            Assert(snapshot["Feature"]?.ToString() == "Checkout", "Feature should be captured");
            Assert(snapshot["UserType"]?.ToString() == "Premium", "UserType should be captured");
            
            CerbiStream.Telemetry.TelemetryContext.Clear();
        });

        runner.RunTest("Telemetry enrichment flag works", () =>
        {
            var options1 = new CerbiStreamOptions().WithTelemetryEnrichment(true);
            Assert(options1.EnableTelemetryEnrichment, "Telemetry enrichment should be enabled");
            
            var options2 = new CerbiStreamOptions().WithTelemetryEnrichment(false);
            Assert(!options2.EnableTelemetryEnrichment, "Telemetry enrichment should be disabled");
        });

        runner.RunTest("Telemetry logging flag works", () =>
        {
            var options1 = new CerbiStreamOptions().WithTelemetryLogging(true);
            Assert(options1.AlsoSendToTelemetry, "Telemetry logging should be enabled");
            
            var options2 = new CerbiStreamOptions().WithTelemetryLogging(false);
            Assert(!options2.AlsoSendToTelemetry, "Telemetry logging should be disabled");
        });

        await Task.CompletedTask;
    }

    private class MockTelemetryProvider : ITelemetryProvider
    {
        public List<string> TrackedEvents { get; } = new();
        public List<Exception> TrackdExceptions { get; } = new();

        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            TrackedEvents.Add(eventName);
        }

        public void TrackException(Exception exception, Dictionary<string, string> properties)
        {
            TrackdExceptions.Add(exception);
        }

        public void TrackDependency(string dependencyName, string command, TimeSpan duration, bool success)
        {
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion failed: {message}");
    }
}
