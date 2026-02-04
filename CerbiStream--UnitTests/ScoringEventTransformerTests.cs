using Cerbi.Contracts.Scoring;
using CerbiStream.GovernanceRuntime.Governance;
using CerbiStream.Logging.Configuration;
using CerbiStream.Services;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace CerbiStream.Tests;

public class ScoringEventTransformerTests
{
    [Fact]
    public void Transform_WithNoViolations_ReturnsNullScore()
    {
        // Arrange
        var options = new CerbiStreamOptions()
            .WithTenantId("test-tenant");
        var logEntry = new { Message = "Test log", LogLevel = "Information" };

        // Act
        var result = ScoringEventTransformer.Transform(logEntry, "log-123", options);

        // Assert
        Assert.Equal("1.1", result.SchemaVersion);
        Assert.Equal("test-tenant", result.TenantId);
        Assert.Null(result.Score); // Score is null - computed by Scoring API
        Assert.Empty(result.Violations ?? new List<ViolationDto>());
    }

    [Fact]
    public void Transform_WithViolations_ExtractsViolationsWithSeverityFromConfig()
    {
        // Arrange
        var options = new CerbiStreamOptions()
            .WithTenantId("test-tenant")
            .WithGovernanceProfile("production");

        var logEntry = new Dictionary<string, object>
        {
            ["Message"] = "Test log",
            ["LogLevel"] = "Warning",
            ["GovernanceViolations"] = new List<Dictionary<string, object>>
            {
                new() 
                { 
                    ["RuleId"] = "PII-001", 
                    ["Code"] = "PII", 
                    ["Field"] = "user.email",
                    ["Severity"] = "Warning",  // Severity comes from governance config
                    ["Message"] = "PII field detected" 
                },
                new() 
                { 
                    ["RuleId"] = "SEC-001", 
                    ["Code"] = "Security",
                    ["Field"] = "password",
                    ["Severity"] = "Critical",  // Severity comes from governance config
                    ["Message"] = "Security field detected"
                }
            }
        };

        // Act
        var result = ScoringEventTransformer.Transform(logEntry, "log-456", options);

        // Assert
        Assert.Null(result.Score); // Score is still null - Scoring API computes it
        Assert.Equal(2, result.Violations?.Count);

        Assert.Equal("PII-001", result.Violations?[0].RuleId);
        Assert.Equal("Warning", result.Violations?[0].Severity); // From config, not hardcoded

        Assert.Equal("SEC-001", result.Violations?[1].RuleId);
        Assert.Equal("Critical", result.Violations?[1].Severity); // From config, not hardcoded
    }

    [Fact]
    public void Transform_ExtractsGovernanceFlags()
    {
        // Arrange
        var options = new CerbiStreamOptions()
            .WithTenantId("test-tenant");

        var logEntry = new Dictionary<string, object>
        {
            ["Message"] = "Relaxed log",
            ["GovernanceRelaxed"] = true
        };

        // Act
        var result = ScoringEventTransformer.Transform(logEntry, "log-789", options);

        // Assert
        Assert.True(result.GovernanceFlags?.GovernanceRelaxed);
    }

    [Fact]
    public void Transform_UsesProfileFromOptions()
    {
        // Arrange
        var options = new CerbiStreamOptions()
            .WithTenantId("my-tenant")
            .WithGovernanceProfile("strict-profile");

        var logEntry = new { Message = "Test" };

        // Act
        var result = ScoringEventTransformer.Transform(logEntry, "log-abc", options);

        // Assert
        Assert.Equal("strict-profile", result.GovernanceProfile);
        Assert.Equal("my-tenant", result.TenantId);
    }

    [Fact]
    public void Transform_DefaultsToUnknownWhenTenantIdNotSet()
    {
        // Arrange
        var options = new CerbiStreamOptions(); // No TenantId set
        var logEntry = new { Message = "Test" };

        // Act
        var result = ScoringEventTransformer.Transform(logEntry, "log-def", options);

        // Assert
        Assert.Equal("unknown", result.TenantId);
    }

    [Fact]
    public void Transform_TenantIdFallback_ConfigFileTakesFirstPriority()
    {
        // Arrange - Create temp config with TenantId
        var configPath = Path.Combine(Path.GetTempPath(), $"cerbi_test_{Guid.NewGuid():N}.json");
        File.WriteAllText(configPath, @"{
            ""TenantId"": ""config-file-tenant"",
            ""LoggingProfiles"": {
                ""default"": { ""DisallowedFields"": [] }
            }
        }");

        try
        {
            var adapter = new GovernanceRuntimeAdapter("default", configPath);
            var options = new CerbiStreamOptions()
                .WithTenantId("code-tenant"); // This should be overridden by config file

            var logEntry = new Dictionary<string, object>
            {
                ["Message"] = "Test",
                ["TenantId"] = "data-tenant" // This should also be overridden
            };

            // Act
            var result = ScoringEventTransformer.Transform(logEntry, "log-xyz", options, null, adapter);

            // Assert - Config file TenantId wins
            Assert.Equal("config-file-tenant", result.TenantId);
        }
        finally
        {
            File.Delete(configPath);
        }
    }

    [Fact]
    public void Transform_TenantIdFallback_OptionsWhenNoConfigFile()
    {
        // Arrange - No config file, use options
        var options = new CerbiStreamOptions()
            .WithTenantId("code-tenant");

        var logEntry = new Dictionary<string, object>
        {
            ["Message"] = "Test",
            ["TenantId"] = "data-tenant"
        };

        // Act - No adapter provided
        var result = ScoringEventTransformer.Transform(logEntry, "log-123", options);

        // Assert - Options TenantId wins (no adapter)
        Assert.Equal("code-tenant", result.TenantId);
    }

    [Fact]
    public void Transform_TenantIdFallback_DataWhenNoOptionsOrConfig()
    {
        // Arrange - No TenantId in options
        var options = new CerbiStreamOptions();

        var logEntry = new Dictionary<string, object>
        {
            ["Message"] = "Test",
            ["TenantId"] = "data-tenant"
        };

        // Act
        var result = ScoringEventTransformer.Transform(logEntry, "log-456", options);

        // Assert - Data TenantId is used
        Assert.Equal("data-tenant", result.TenantId);
    }
}
