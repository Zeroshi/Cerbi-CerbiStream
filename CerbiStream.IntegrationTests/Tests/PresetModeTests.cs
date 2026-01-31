using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CerbiStream.Configuration;
using CerbiStream.Logging.Configuration;

namespace CerbiStream.IntegrationTests.Tests;

/// <summary>
/// Tests for all preset modes: EnableDeveloperMode, ForProduction, ForTesting, ForPerformance
/// </summary>
public static class PresetModeTests
{
    public static async Task RunAllAsync(TestRunner runner)
    {
        Console.WriteLine("\n🎯 Preset Mode Tests");
        Console.WriteLine("   Testing all preset configurations...\n");

        runner.RunTest("EnableDeveloperMode() configures correctly", () =>
        {
            var options = new CerbiStreamOptions().EnableDeveloperMode();
            
            Assert(options.EnableGovernanceChecks, "Governance should be enabled in dev mode");
            Assert(options.DisableQueueSending, "Queue should be disabled in dev mode");
            Assert(options.EnableConsoleOutput, "Console output should be enabled in dev mode");
            Assert(options.MinimalMode, "Minimal mode should be enabled in dev mode");
        });

        runner.RunTest("ForProduction() configures correctly", () =>
        {
            var options = new CerbiStreamOptions().ForProduction();
            
            Assert(options.EnableGovernanceChecks, "Governance should be enabled in production");
            Assert(!options.DisableQueueSending, "Queue should be enabled in production");
            Assert(!options.EnableConsoleOutput, "Console output should be disabled in production");
            Assert(options.EnableTelemetryEnrichment, "Telemetry should be enabled in production");
            Assert(options.EnableMetadataInjection, "Metadata injection should be enabled in production");
            Assert(options.FullMode, "Full mode should be enabled in production");
        });

        runner.RunTest("ForTesting() configures correctly", () =>
        {
            var options = new CerbiStreamOptions().ForTesting();
            
            Assert(options.EnableGovernanceChecks, "Governance should be enabled in testing");
            Assert(options.DisableQueueSending, "Queue should be disabled in testing");
            Assert(options.EnableConsoleOutput, "Console output should be enabled in testing");
            Assert(options.EnableMetadataInjection, "Metadata injection should be enabled in testing");
        });

        runner.RunTest("ForPerformance() configures correctly", () =>
        {
            var options = new CerbiStreamOptions().ForPerformance();
            
            Assert(!options.EnableGovernanceChecks, "Governance should be disabled for performance");
            Assert(options.DisableQueueSending, "Queue should be disabled for performance");
            Assert(!options.EnableConsoleOutput, "Console output should be disabled for performance");
            Assert(!options.EnableTelemetryEnrichment, "Telemetry should be disabled for performance");
            Assert(!options.EnableMetadataInjection, "Metadata injection should be disabled for performance");
            Assert(options.IsBenchmarkMode, "Should be in benchmark mode");
        });

        runner.RunTest("Preset modes can be chained with other options", () =>
        {
            var options = new CerbiStreamOptions()
                .ForProduction()
                .WithGovernanceProfile("myapp")
                .WithQueueRetries(true, 5, 500);
            
            Assert(options.GovernanceProfileName == "myapp", "Profile should be customized");
            Assert(options.EnableQueueRetries, "Retries should be enabled");
            Assert(options.QueueRetryCount == 5, "Retry count should be 5");
        });

        runner.RunTest("Each preset works in AddCerbiStream", () =>
        {
            var presets = new Action<CerbiStreamOptions>[]
            {
                o => o.EnableDeveloperMode(),
                o => o.ForProduction(),
                o => o.ForTesting(),
                o => o.ForPerformance()
            };

            foreach (var preset in presets)
            {
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddCerbiStream(preset));
                
                using var provider = services.BuildServiceProvider();
                var options = provider.GetService<CerbiStreamOptions>();
                Assert(options != null, "Options should be registered for each preset");
            }
        });

        await Task.CompletedTask;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion failed: {message}");
    }
}
