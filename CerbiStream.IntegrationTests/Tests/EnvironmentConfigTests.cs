using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CerbiStream.Configuration;
using CerbiStream.Logging.Configuration;

namespace CerbiStream.IntegrationTests.Tests;

/// <summary>
/// Tests for environment variable configuration
/// </summary>
public static class EnvironmentConfigTests
{
    public static async Task RunAllAsync(TestRunner runner)
    {
        Console.WriteLine("\n🌍 Environment Variable Configuration Tests");
        Console.WriteLine("   Testing env var based configuration...\n");

        // Store original env vars to restore later
        var originalVars = new Dictionary<string, string?>();

        try
        {
            runner.RunTest("FromEnvironment() with no vars uses defaults", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);

                var options = new CerbiStreamOptions().FromEnvironment();

                // Should have sensible defaults (not crash)
                Assert(options != null, "Options should be created");
            });

            runner.RunTest("CERBISTREAM_MODE=development configures dev mode", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_MODE", "development");

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(options.EnableConsoleOutput, "Console should be enabled in dev mode");
                Assert(options.DisableQueueSending, "Queue should be disabled in dev mode");
                Assert(options.EnableGovernanceChecks, "Governance should be enabled in dev mode");
            });

            runner.RunTest("CERBISTREAM_MODE=production configures prod mode", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_MODE", "production");

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(!options.EnableConsoleOutput, "Console should be disabled in prod mode");
                Assert(!options.DisableQueueSending, "Queue should be enabled in prod mode");
                Assert(options.EnableGovernanceChecks, "Governance should be enabled in prod mode");
                Assert(options.EnableTelemetryEnrichment, "Telemetry should be enabled in prod mode");
            });

            runner.RunTest("CERBISTREAM_MODE=testing configures test mode", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_MODE", "testing");

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(options.EnableConsoleOutput, "Console should be enabled in test mode");
                Assert(options.DisableQueueSending, "Queue should be disabled in test mode");
                Assert(options.EnableGovernanceChecks, "Governance should be enabled in test mode");
            });

            runner.RunTest("CERBISTREAM_MODE=performance configures perf mode", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_MODE", "performance");

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(!options.EnableConsoleOutput, "Console should be disabled in perf mode");
                Assert(options.DisableQueueSending, "Queue should be disabled in perf mode");
                Assert(!options.EnableGovernanceChecks, "Governance should be disabled in perf mode");
                Assert(options.IsBenchmarkMode, "Should be in benchmark mode");
            });

            runner.RunTest("Individual env vars override mode settings", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_MODE", "production");
                Environment.SetEnvironmentVariable("CERBISTREAM_CONSOLE_OUTPUT", "true"); // Override!

                var options = new CerbiStreamOptions().FromEnvironment();

                // Mode says no console, but override says yes
                Assert(options.EnableConsoleOutput, "Console override should take precedence");
            });

            runner.RunTest("CERBISTREAM_QUEUE_ENABLED controls queue sending", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_QUEUE_ENABLED", "true");

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(!options.DisableQueueSending, "Queue should be enabled");
            });

            runner.RunTest("CERBISTREAM_GOVERNANCE_PROFILE sets profile name", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_GOVERNANCE_PROFILE", "myapp");

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(options.GovernanceProfileName == "myapp", "Profile should be 'myapp'");
            });

            runner.RunTest("Queue configuration from env vars", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_QUEUE_TYPE", "AzureServiceBus");
                Environment.SetEnvironmentVariable("CERBISTREAM_QUEUE_CONNECTION", "Endpoint=sb://test");
                Environment.SetEnvironmentVariable("CERBISTREAM_QUEUE_NAME", "logs-queue");

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(options.QueueType == "AzureServiceBus", "Queue type should match");
                Assert(options.QueueHost == "Endpoint=sb://test", "Queue connection should match");
                Assert(options.QueueName == "logs-queue", "Queue name should match");
            });

            runner.RunTest("Retry configuration from env vars", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_QUEUE_RETRIES_ENABLED", "true");
                Environment.SetEnvironmentVariable("CERBISTREAM_QUEUE_RETRY_COUNT", "5");
                Environment.SetEnvironmentVariable("CERBISTREAM_QUEUE_RETRY_DELAY_MS", "1000");

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(options.EnableQueueRetries, "Retries should be enabled");
                Assert(options.QueueRetryCount == 5, "Retry count should be 5");
                Assert(options.QueueRetryDelayMilliseconds == 1000, "Retry delay should be 1000");
            });

            runner.RunTest("Encryption mode from env vars", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_ENCRYPTION_MODE", "AES");

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(options.EncryptionMode == CerbiStream.Interfaces.IEncryptionTypeProvider.EncryptionType.AES,
                    "Encryption mode should be AES");
            });

            runner.RunTest("Encryption key/IV from base64 env vars", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                var key = new byte[16];
                var iv = new byte[16];
                new Random(42).NextBytes(key);
                new Random(43).NextBytes(iv);

                Environment.SetEnvironmentVariable("CERBISTREAM_ENCRYPTION_MODE", "AES");
                Environment.SetEnvironmentVariable("CERBISTREAM_ENCRYPTION_KEY", Convert.ToBase64String(key));
                Environment.SetEnvironmentVariable("CERBISTREAM_ENCRYPTION_IV", Convert.ToBase64String(iv));

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(options.EncryptionKey != null, "Key should be set");
                Assert(options.EncryptionIV != null, "IV should be set");
                Assert(options.EncryptionKey.Length == 16, "Key should be 16 bytes");
            });

            runner.RunTest("File fallback from env vars", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_FILE_FALLBACK_ENABLED", "true");
                Environment.SetEnvironmentVariable("CERBISTREAM_FILE_FALLBACK_PATH", "/var/log/fallback.json");
                Environment.SetEnvironmentVariable("CERBISTREAM_FILE_PRIMARY_PATH", "/var/log/primary.json");

                var options = new CerbiStreamOptions().FromEnvironment();

                Assert(options.FileFallback != null, "File fallback should be configured");
                Assert(options.FileFallback.Enable, "File fallback should be enabled");
                Assert(options.FileFallback.FallbackFilePath == "/var/log/fallback.json", "Fallback path should match");
            });

            runner.RunTest("AddCerbiStream auto-detects environment", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_MODE", "production");

                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddCerbiStream()); // No explicit config!

                using var provider = services.BuildServiceProvider();
                var options = provider.GetRequiredService<CerbiStreamOptions>();

                // Should have picked up production mode from env
                Assert(!options.DisableQueueSending, "Should detect production mode from env");
            });

            runner.RunTest("AddCerbiStreamFromEnvironment explicit method works", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_MODE", "testing");

                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddCerbiStreamFromEnvironment());

                using var provider = services.BuildServiceProvider();
                var options = provider.GetRequiredService<CerbiStreamOptions>();

                Assert(options.EnableGovernanceChecks, "Should be in testing mode");
            });

            runner.RunTest("Code config can override environment config", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_MODE", "production");
                Environment.SetEnvironmentVariable("CERBISTREAM_GOVERNANCE_PROFILE", "env-profile");

                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddCerbiStream(o => o
                    .FromEnvironment()
                    .WithGovernanceProfile("code-profile"))); // Override env!

                using var provider = services.BuildServiceProvider();
                var options = provider.GetRequiredService<CerbiStreamOptions>();

                Assert(options.GovernanceProfileName == "code-profile", "Code should override env");
            });

            runner.RunTest("GetEnvironmentDiagnostics returns set variables", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_MODE", "production");
                Environment.SetEnvironmentVariable("CERBISTREAM_GOVERNANCE_PROFILE", "myapp");

                var diag = CerbiStreamOptions.GetEnvironmentDiagnostics();

                Assert(diag.ContainsKey("CERBISTREAM_MODE"), "Should contain MODE");
                Assert(diag["CERBISTREAM_MODE"] == "production", "MODE should be production");
                Assert(diag.ContainsKey("CERBISTREAM_GOVERNANCE_PROFILE"), "Should contain PROFILE");
            });

            runner.RunTest("Sensitive env vars are masked in diagnostics", () =>
            {
                ClearAllCerbiStreamEnvVars(originalVars);
                Environment.SetEnvironmentVariable("CERBISTREAM_ENCRYPTION_KEY", "c2VjcmV0");
                Environment.SetEnvironmentVariable("CERBISTREAM_QUEUE_CONNECTION", "secret-connection");

                var diag = CerbiStreamOptions.GetEnvironmentDiagnostics();

                Assert(diag["CERBISTREAM_ENCRYPTION_KEY"] == "***SET***", "Key should be masked");
                Assert(diag["CERBISTREAM_QUEUE_CONNECTION"] == "***SET***", "Connection should be masked");
            });

            runner.RunTest("Boolean parsing handles various formats", () =>
            {
                // Test true values
                foreach (var trueVal in new[] { "true", "TRUE", "1", "yes", "YES", "on", "ON" })
                {
                    ClearAllCerbiStreamEnvVars(originalVars);
                    Environment.SetEnvironmentVariable("CERBISTREAM_CONSOLE_OUTPUT", trueVal);
                    var options = new CerbiStreamOptions().FromEnvironment();
                    Assert(options.EnableConsoleOutput, $"'{trueVal}' should parse as true");
                }

                // Test false values
                foreach (var falseVal in new[] { "false", "FALSE", "0", "no", "NO", "off", "OFF" })
                {
                    ClearAllCerbiStreamEnvVars(originalVars);
                    Environment.SetEnvironmentVariable("CERBISTREAM_CONSOLE_OUTPUT", falseVal);
                    var options = new CerbiStreamOptions().FromEnvironment();
                    Assert(!options.EnableConsoleOutput, $"'{falseVal}' should parse as false");
                }
            });
        }
        finally
        {
            // Restore original environment
            RestoreEnvVars(originalVars);
        }

        await Task.CompletedTask;
    }

    private static void ClearAllCerbiStreamEnvVars(Dictionary<string, string?> backup)
    {
        var vars = new[]
        {
            "CERBISTREAM_MODE",
            "CERBISTREAM_GOVERNANCE_ENABLED", "CERBISTREAM_GOVERNANCE_PROFILE", "CERBI_GOVERNANCE_PATH",
            "CERBISTREAM_QUEUE_ENABLED", "CERBISTREAM_QUEUE_TYPE", "CERBISTREAM_QUEUE_CONNECTION", "CERBISTREAM_QUEUE_NAME",
            "CERBISTREAM_QUEUE_RETRIES_ENABLED", "CERBISTREAM_QUEUE_RETRY_COUNT", "CERBISTREAM_QUEUE_RETRY_DELAY_MS",
            "CERBISTREAM_ENCRYPTION_MODE", "CERBISTREAM_ENCRYPTION_KEY", "CERBISTREAM_ENCRYPTION_IV",
            "CERBISTREAM_CONSOLE_OUTPUT", "CERBISTREAM_TELEMETRY_ENABLED", "CERBISTREAM_TELEMETRY_ENRICHMENT", "CERBISTREAM_METADATA_INJECTION",
            "CERBISTREAM_FILE_FALLBACK_ENABLED", "CERBISTREAM_FILE_PRIMARY_PATH", "CERBISTREAM_FILE_FALLBACK_PATH"
        };

        foreach (var v in vars)
        {
            if (!backup.ContainsKey(v))
                backup[v] = Environment.GetEnvironmentVariable(v);
            Environment.SetEnvironmentVariable(v, null);
        }
    }

    private static void RestoreEnvVars(Dictionary<string, string?> backup)
    {
        foreach (var kv in backup)
        {
            Environment.SetEnvironmentVariable(kv.Key, kv.Value);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion failed: {message}");
    }
}
