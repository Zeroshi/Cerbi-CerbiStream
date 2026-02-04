# CerbiStream MEL Integration - Scoring Contract Implementation

**Package:** `CerbiStream.Extensions.Logging`  
**Target:** Microsoft.Extensions.Logging  
**Date:** 2026-02-03  
**Status:** ?? Implementation Guide

---

## Overview

MEL (Microsoft.Extensions.Logging) integration for CerbiStream. The SDK sends violations to the Scoring API - it does **NOT** compute scores.

**Key Principle:** Scoring is centralized in the Scoring API using governance configuration. SDKs only send violations with severities from `cerbi_governance.json`.

---

## Architecture

```
ILogger.LogInformation(...)
         ?
         ?
???????????????????????????????????????
?    CerbiStreamLoggerProvider        ?
?    - Intercepts log calls           ?
?    - Runs governance validation     ?
?    - Extracts violations + severity ?
?    - Sends to queue                 ?
???????????????????????????????????????
         ?
         ?  ScoringEventDto (Score = null)
         ?
???????????????????????????????????????
?         Scoring API                 ?
?    - Reads governance config        ?
?    - Computes scores centrally      ?
???????????????????????????????????????
```

---

## What Gets Sent

```json
{
  "SchemaVersion": "1.0",
  "TenantId": "my-tenant",
  "AppName": "MyWebApi",
  "LogId": "guid",
  "Score": null,
  "Violations": [
    {
      "RuleId": "PII-001",
      "Severity": "Warning"
    }
  ]
}
```

- `Score` is **null** - Scoring API computes it
- `Severity` comes from governance config, not SDK

---

## Changes Required

### 1. Package References

```xml
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Cerbi.Contracts" Version="1.1.0" />
<PackageReference Include="Cerbi.Governance.Runtime" Version="1.1.7" />
```

### 2. Options Class

**File:** `CerbiStreamLoggerOptions.cs`

```csharp
namespace CerbiStream.Extensions.Logging;

public class CerbiStreamLoggerOptions
{
    // Required for routing
    public string TenantId { get; set; } = "unknown";
    public string ApplicationName { get; set; } = "unknown";
    public string Environment { get; set; } = "unknown";
    
    // Governance config
    public string GovernanceProfile { get; set; } = "default";
    public string? GovernanceConfigPath { get; set; }
    
    // Queue settings
    public string QueueType { get; set; } = "AzureServiceBus";
    public string QueueConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = "cerbishield.log-scoring";
    
    // Filtering
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    
    // NO scoring configuration - that's in governance config + Scoring API
}
```

### 3. Logger Implementation

**File:** `CerbiStreamLogger.cs`

```csharp
using Cerbi.Contracts.Scoring;
using Cerbi.Governance;

internal class CerbiStreamLogger : ILogger
{
    private readonly string _categoryName;
    private readonly CerbiStreamLoggerOptions _options;
    private readonly IQueueSender _queueSender;
    private readonly RuntimeGovernanceValidator _governanceValidator;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var stateDict = ExtractState(state);

        // Run governance validation - severities come from config
        var violations = new List<ViolationDto>();
        if (_governanceValidator != null)
        {
            var result = _governanceValidator.Validate(stateDict);
            violations = result.Violations.Select(v => new ViolationDto
            {
                RuleId = v.RuleId,
                Code = v.Code,
                Field = v.Field,
                Severity = v.Severity,  // FROM GOVERNANCE CONFIG
                Message = v.Message
            }).ToList();
        }

        // Build DTO - Score is null, API computes it
        var scoringEvent = new ScoringEventDto
        {
            SchemaVersion = ContractVersions.ScoringEventSchemaVersion,
            TenantId = _options.TenantId,
            AppName = _options.ApplicationName,
            Environment = _options.Environment,
            Runtime = "dotnet",
            LogId = Guid.NewGuid().ToString(),
            TimestampUtc = DateTime.UtcNow,
            GovernanceProfile = _options.GovernanceProfile,
            LogLevel = logLevel.ToString(),
            
            Score = null,  // Scoring API computes this
            
            Violations = violations,
            GovernanceFlags = new GovernanceFlagsDto
            {
                GovernanceRelaxed = false
            },
            RawPayload = BuildPayload(message, stateDict, exception)
        };

        // Send to queue
        var json = JsonSerializer.Serialize(scoringEvent);
        _queueSender.SendAsync(json, scoringEvent.LogId);
    }
}
```

### 4. Extension Method

**File:** `LoggingBuilderExtensions.cs`

```csharp
public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddCerbiStream(
        this ILoggingBuilder builder,
        Action<CerbiStreamLoggerOptions> configure)
    {
        var options = new CerbiStreamLoggerOptions();
        configure(options);

        // Initialize governance validator with config file
        var governanceValidator = new RuntimeGovernanceValidator(
            isEnabled: () => true,
            profileName: options.GovernanceProfile,
            source: new FileGovernanceSource(
                options.GovernanceConfigPath ?? "cerbi_governance.json"
            )
        );

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(governanceValidator);
        builder.Services.AddSingleton<ILoggerProvider, CerbiStreamLoggerProvider>();

        return builder;
    }
}
```

---

## Usage

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.TenantId = "my-tenant";
    options.ApplicationName = "MyWebApi";
    options.Environment = builder.Environment.EnvironmentName;
    options.GovernanceProfile = "default";
    options.QueueConnectionString = config["ServiceBus:ConnectionString"];
});
```

**Developer does NOT configure:**
- Severity weights
- Score deductions  
- Rule definitions

All that is in `cerbi_governance.json` and Scoring API.

---

## Testing

Verify:
1. `Score` field is `null` in output
2. `Severity` on violations matches governance config
3. Violations are extracted correctly
