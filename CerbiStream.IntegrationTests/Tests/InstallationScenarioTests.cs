using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CerbiStream.Configuration;
using CerbiStream.Logging.Configuration;
using CerbiStream.Classes.OpenTelemetry;
using FileFallbackOptions = CerbiStream.Classes.FileLogging.FileFallbackOptions;

namespace CerbiStream.IntegrationTests.Tests;

/// <summary>
/// Tests for all installation scenarios to verify they work correctly
/// </summary>
public static class InstallationScenarioTests
{
    public static async Task RunAllAsync(TestRunner runner)
    {
        Console.WriteLine("\n📦 Installation Scenario Tests");
        Console.WriteLine("   Testing all documented installation paths...\n");

        // Scenario 1: Zero-config
        runner.RunTest("Scenario 1: Zero-config installation works", () =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddCerbiStream());
            
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<CerbiStreamOptions>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Test");
            
            // Should not throw
            logger.LogInformation("Test message from zero-config");
            
            Assert(options != null, "Options should be registered");
            Assert(options.EnableGovernanceChecks, "Governance should be enabled by default");
        });

        // Scenario 2: With Governance
        runner.RunTest("Scenario 2: ForProduction() with governance works", () =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddCerbiStream(o => o
                .ForProduction()
                .WithGovernanceProfile("myapp")));
            
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<CerbiStreamOptions>();
            
            Assert(options.EnableGovernanceChecks, "Governance should be enabled");
            Assert(options.GovernanceProfileName == "myapp", "Profile should be 'myapp'");
            Assert(!options.DisableQueueSending, "Queue should be enabled in production");
        });

        // Scenario 3: With Azure App Insights
        runner.RunTest("Scenario 3: With AppInsights telemetry works", () =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddCerbiStream(o => o
                .ForProduction()
                .WithTelemetryProvider(new AppInsightsTelemetryProvider())
                .WithTelemetryLogging(true)
                .WithTelemetryEnrichment(true)));
            
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<CerbiStreamOptions>();
            
            Assert(options.TelemetryProvider != null, "Telemetry provider should be set");
            Assert(options.TelemetryProvider is AppInsightsTelemetryProvider, "Should be AppInsights provider");
            Assert(options.AlsoSendToTelemetry, "Telemetry logging should be enabled");
            Assert(options.EnableTelemetryEnrichment, "Telemetry enrichment should be enabled");
        });

        // Scenario 4: With File Fallback
        runner.RunTest("Scenario 4: With file fallback works", () =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddCerbiStream(o => o
                .ForProduction()
                .WithFileFallback("logs/fallback.json", "logs/primary.json")));
            
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<CerbiStreamOptions>();
            
            Assert(options.FileFallback != null, "FileFallback should be configured");
            Assert(options.FileFallback.Enable, "FileFallback should be enabled");
            Assert(options.FileFallback.FallbackFilePath == "logs/fallback.json", "Fallback path should match");
            Assert(options.FileFallback.PrimaryFilePath == "logs/primary.json", "Primary path should match");
        });

        // Scenario 4b: With encrypted file fallback
        runner.RunTest("Scenario 4b: With encrypted file fallback works", () =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddCerbiStream(o => o
                .ForProduction()
                .WithFileFallback(new FileFallbackOptions
                {
                    Enable = true,
                    PrimaryFilePath = "logs/primary.json",
                    FallbackFilePath = "logs/fallback.json",
                    MaxFileSizeBytes = 10 * 1024 * 1024,
                    MaxFileAge = TimeSpan.FromMinutes(30),
                    EncryptionKey = "1234567890123456",
                    EncryptionIV = "1234567890123456"
                })
                .WithAesEncryption()));
            
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<CerbiStreamOptions>();
            
            Assert(options.FileFallback != null, "FileFallback should be configured");
            Assert(options.FileFallback.EncryptionKey != null, "Encryption key should be set");
            Assert(options.EncryptionMode == CerbiStream.Interfaces.IEncryptionTypeProvider.EncryptionType.AES, 
                "Encryption mode should be AES");
        });

        // Scenario 5: Full production with queue
        runner.RunTest("Scenario 5: Full production setup with queue works", () =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddCerbiStream(o => o
                .ForProduction()
                .WithGovernanceProfile("production")
                .WithGovernanceChecks(true)
                .WithQueue("AzureServiceBus", "connection-string", "logs-queue")
                .WithQueueRetries(true, retryCount: 5, delayMilliseconds: 500)
                .WithTelemetryProvider(new AppInsightsTelemetryProvider())
                .WithTelemetryLogging(true)
                .WithFileFallback("logs/fallback.json", "logs/primary.json")));
            
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<CerbiStreamOptions>();
            
            // Governance
            Assert(options.EnableGovernanceChecks, "Governance should be enabled");
            Assert(options.GovernanceProfileName == "production", "Profile should be 'production'");
            
            // Queue
            Assert(!options.DisableQueueSending, "Queue should be enabled");
            Assert(options.QueueType == "AzureServiceBus", "Queue type should match");
            Assert(options.EnableQueueRetries, "Queue retries should be enabled");
            Assert(options.QueueRetryCount == 5, "Retry count should be 5");
            
            // Telemetry
            Assert(options.TelemetryProvider != null, "Telemetry should be configured");
            Assert(options.AlsoSendToTelemetry, "Telemetry logging should be enabled");
            
            // File fallback
            Assert(options.FileFallback != null, "File fallback should be configured");
        });

        // Scenario 6: Queue scoring verification
        runner.RunTest("Scenario 6: Queue sends governance scoring metadata", () =>
        {
            // Verify that governance metadata (violations, profile, etc.) is included in logs
            var configPath = CreateTempGovernanceConfig(@"{
                ""Version"": ""1.0"",
                ""LoggingProfiles"": {
                    ""default"": {
                        ""DisallowedFields"": [""ssn""],
                        ""FieldSeverities"": {}
                    }
                }
            }");

            try
            {
                var adapter = new CerbiStream.GovernanceRuntime.Governance.GovernanceRuntimeAdapter("default", configPath);
                var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ssn"] = "123-45-6789",
                    ["userId"] = "user123"
                };

                adapter.ValidateAndRedactInPlace(data);

                // Verify governance scoring metadata is added
                Assert(data.ContainsKey("GovernanceViolations"), "Should have GovernanceViolations");
                Assert(data.ContainsKey("GovernanceProfileUsed") || data.ContainsKey("GovernanceEnforced"), 
                    "Should have governance metadata");
                Assert(data["ssn"]?.ToString() == "***REDACTED***", "SSN should be redacted");
                
                // This metadata would be sent to the queue for scoring/analytics
            }
            finally
            {
                CleanupTempFile(configPath);
            }
        });

        // Scenario 7: Preset chaining
        runner.RunTest("Scenario 7: All presets work and can be chained", () =>
        {
            // Test that presets can be mixed with custom options
            var scenarios = new (string Name, Action<CerbiStreamOptions> Configure)[]
            {
                ("Developer", o => o.EnableDeveloperMode().WithGovernanceProfile("dev")),
                ("Production", o => o.ForProduction().WithQueueRetries(true, 3, 100)),
                ("Testing", o => o.ForTesting().WithConsoleOutput(true)),
                ("Performance", o => o.ForPerformance())
            };

            foreach (var (name, configure) in scenarios)
            {
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddCerbiStream(configure));
                
                using var provider = services.BuildServiceProvider();
                var options = provider.GetService<CerbiStreamOptions>();
                Assert(options != null, $"{name} preset should work");
            }
        });

        await Task.CompletedTask;
    }

    private static string CreateTempGovernanceConfig(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"cerbi_gov_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }

    private static void CleanupTempFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion failed: {message}");
    }
}
