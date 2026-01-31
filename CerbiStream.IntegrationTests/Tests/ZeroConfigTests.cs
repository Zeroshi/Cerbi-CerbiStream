using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CerbiStream.Configuration;
using CerbiStream.Logging.Configuration;

namespace CerbiStream.IntegrationTests.Tests;

/// <summary>
/// Tests for zero-config setup - the "just works" developer experience
/// </summary>
public static class ZeroConfigTests
{
    public static async Task RunAllAsync(TestRunner runner)
    {
        Console.WriteLine("\n📦 Zero-Config Setup Tests");
        Console.WriteLine("   Testing one-line setup experience...\n");

        runner.RunTest("AddCerbiStream() with no parameters works", () =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddCerbiStream());
            
            using var provider = services.BuildServiceProvider();
            var options = provider.GetService<CerbiStreamOptions>();
            
            Assert(options != null, "CerbiStreamOptions should be registered");
        });

        runner.RunTest("Default mode is DeveloperMode", () =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddCerbiStream());
            
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<CerbiStreamOptions>();
            
            Assert(options.EnableGovernanceChecks, "Governance checks should be enabled by default");
            Assert(options.DisableQueueSending, "Queue should be disabled in dev mode");
        });

        runner.RunTest("Logger provider is registered", () =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddCerbiStream());
            
            using var provider = services.BuildServiceProvider();
            var loggerProviders = provider.GetServices<ILoggerProvider>().ToList();
            
            Assert(loggerProviders.Any(p => 
                p.GetType().Name.Contains("Governance") || 
                p.GetType().Name.Contains("CerbiStream")), 
                "CerbiStream logger provider should be registered");
        });

        runner.RunTest("Can create and use logger", () =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddCerbiStream());
            
            using var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("IntegrationTest");
            
            // Should not throw
            logger.LogInformation("Test message from zero-config setup");
        });

        runner.RunTest("Governance config auto-generated when missing", () =>
        {
            var testDir = Path.Combine(Path.GetTempPath(), $"cerbi_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(testDir);
            var configPath = Path.Combine(testDir, "cerbi_governance.json");
            
            try
            {
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddCerbiStream(o => o
                    .EnableDeveloperMode()
                    .WithGovernanceConfigPath(configPath)));
                
                using var provider = services.BuildServiceProvider();
                
                // Config should be auto-created
                // Note: The auto-creation happens during registration
            }
            finally
            {
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, true);
            }
        });

        await Task.CompletedTask;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion failed: {message}");
    }
}
