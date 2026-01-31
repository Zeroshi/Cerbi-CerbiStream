using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CerbiStream.Configuration;
using CerbiStream.GovernanceRuntime.Governance;

namespace CerbiStream.IntegrationTests.Tests;

/// <summary>
/// Tests for governance and PII redaction functionality
/// </summary>
public static class GovernanceTests
{
    public static async Task RunAllAsync(TestRunner runner)
    {
        Console.WriteLine("\n🛡️ Governance & Redaction Tests");
        Console.WriteLine("   Testing PII protection and governance rules...\n");

        runner.RunTest("GovernanceRuntimeAdapter can be created", () =>
        {
            var configPath = CreateTempGovernanceConfig();
            try
            {
                var adapter = new GovernanceRuntimeAdapter("default", configPath);
                Assert(adapter != null, "Adapter should be created");
            }
            finally
            {
                CleanupTempFile(configPath);
            }
        });

        runner.RunTest("Disallowed fields are redacted", () =>
        {
            var configPath = CreateTempGovernanceConfig(@"{
                ""Version"": ""1.0"",
                ""LoggingProfiles"": {
                    ""default"": {
                        ""DisallowedFields"": [""ssn"", ""password""],
                        ""FieldSeverities"": {}
                    }
                }
            }");

            try
            {
                var adapter = new GovernanceRuntimeAdapter("default", configPath);
                var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ssn"] = "123-45-6789",
                    ["userId"] = "user123",
                    ["password"] = "secret123"
                };

                adapter.ValidateAndRedactInPlace(data);

                Assert(data["ssn"]?.ToString() == "***REDACTED***", "SSN should be redacted");
                Assert(data["password"]?.ToString() == "***REDACTED***", "Password should be redacted");
                Assert(data["userId"]?.ToString() == "user123", "UserId should NOT be redacted");
            }
            finally
            {
                CleanupTempFile(configPath);
            }
        });

        runner.RunTest("Governance violations are tagged", () =>
        {
            var configPath = CreateTempGovernanceConfig(@"{
                ""Version"": ""1.0"",
                ""LoggingProfiles"": {
                    ""default"": {
                        ""DisallowedFields"": [""creditCard""],
                        ""FieldSeverities"": {}
                    }
                }
            }");

            try
            {
                var adapter = new GovernanceRuntimeAdapter("default", configPath);
                var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["creditCard"] = "4111-1111-1111-1111",
                    ["amount"] = 99.99
                };

                adapter.ValidateAndRedactInPlace(data);

                Assert(data.ContainsKey("GovernanceViolations"), "Should contain violations tag");
            }
            finally
            {
                CleanupTempFile(configPath);
            }
        });

        runner.RunTest("Relaxed mode bypasses redaction", () =>
        {
            var configPath = CreateTempGovernanceConfig(@"{
                ""Version"": ""1.0"",
                ""LoggingProfiles"": {
                    ""default"": {
                        ""DisallowedFields"": [""secret""],
                        ""FieldSeverities"": {}
                    }
                }
            }");

            try
            {
                var adapter = new GovernanceRuntimeAdapter("default", configPath);
                var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["GovernanceRelaxed"] = true,
                    ["secret"] = "mysecret"
                };

                adapter.ValidateAndRedactInPlace(data);

                // With relaxed mode, the value should NOT be redacted
                Assert(data["secret"]?.ToString() == "mysecret", "Secret should NOT be redacted in relaxed mode");
            }
            finally
            {
                CleanupTempFile(configPath);
            }
        });

        runner.RunTest("Default auto-generated policy redacts common PII fields", () =>
        {
            // Test the default fields that should be in auto-generated policy
            var defaultPiiFields = new[] { "password", "ssn", "creditCard", "secret", "token", "apiKey" };
            
            var configPath = CreateTempGovernanceConfig(@"{
                ""Version"": ""1.0"",
                ""LoggingProfiles"": {
                    ""default"": {
                        ""DisallowedFields"": [""password"", ""ssn"", ""creditCard"", ""secret"", ""token"", ""apiKey""],
                        ""FieldSeverities"": {}
                    }
                }
            }");

            try
            {
                var adapter = new GovernanceRuntimeAdapter("default", configPath);
                
                foreach (var field in defaultPiiFields)
                {
                    var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        [field] = "sensitive_value"
                    };
                    
                    adapter.ValidateAndRedactInPlace(data);
                    Assert(data[field]?.ToString() == "***REDACTED***", $"{field} should be redacted");
                }
            }
            finally
            {
                CleanupTempFile(configPath);
            }
        });

        runner.RunTest("Case-insensitive field matching", () =>
        {
            var configPath = CreateTempGovernanceConfig(@"{
                ""Version"": ""1.0"",
                ""LoggingProfiles"": {
                    ""default"": {
                        ""DisallowedFields"": [""SSN""],
                        ""FieldSeverities"": {}
                    }
                }
            }");

            try
            {
                var adapter = new GovernanceRuntimeAdapter("default", configPath);
                var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ssn"] = "123-45-6789"  // lowercase
                };

                adapter.ValidateAndRedactInPlace(data);

                Assert(data["ssn"]?.ToString() == "***REDACTED***", "SSN should be redacted (case-insensitive)");
            }
            finally
            {
                CleanupTempFile(configPath);
            }
        });

        await Task.CompletedTask;
    }

    private static string CreateTempGovernanceConfig(string? content = null)
    {
        content ??= @"{
            ""Version"": ""1.0"",
            ""LoggingProfiles"": {
                ""default"": {
                    ""DisallowedFields"": [],
                    ""FieldSeverities"": {}
                }
            }
        }";

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
