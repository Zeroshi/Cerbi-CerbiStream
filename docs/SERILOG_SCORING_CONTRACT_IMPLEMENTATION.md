# CerbiStream Serilog Sink - Scoring Contract Implementation

**Package:** `CerbiStream.Sinks.Serilog`  
**Target:** Serilog  
**Date:** 2026-02-03  
**Status:** ?? Implementation Guide

---

## Overview

Serilog sink for CerbiStream. The sink sends violations to the Scoring API - it does **NOT** compute scores.

**Key Principle:** Scoring is centralized in the Scoring API. SDKs only send violations with severities defined in `cerbi_governance.json`.

---

## Architecture

```
Log.Information("...")
         ?
         ?
???????????????????????????????????????
?      CerbiStreamSink                ?
?    - Receives LogEvent              ?
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
  "AppName": "MyApp",
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
- `Severity` comes from governance config

---

## Changes Required

### 1. Package References

```xml
<PackageReference Include="Serilog" Version="4.0.0" />
<PackageReference Include="Cerbi.Contracts" Version="1.1.0" />
<PackageReference Include="Cerbi.Governance.Runtime" Version="1.1.7" />
```

### 2. Options Class

**File:** `CerbiStreamSinkOptions.cs`

```csharp
using Serilog.Events;

namespace CerbiStream.Sinks.Serilog;

public class CerbiStreamSinkOptions
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
    public LogEventLevel RestrictedToMinimumLevel { get; set; } = LogEventLevel.Information;
    
    // NO scoring configuration - that's handled by governance config + Scoring API
}
```

### 3. Sink Implementation

**File:** `CerbiStreamSink.cs`

```csharp
using Cerbi.Contracts;
using Cerbi.Contracts.Scoring;
using Cerbi.Governance;
using Serilog.Core;
using Serilog.Events;

namespace CerbiStream.Sinks.Serilog;

public class CerbiStreamSink : ILogEventSink, IDisposable
{
    private readonly CerbiStreamSinkOptions _options;
    private readonly IQueueSender _queueSender;
    private readonly RuntimeGovernanceValidator _governanceValidator;

    public CerbiStreamSink(CerbiStreamSinkOptions options)
    {
        _options = options;
        _queueSender = CreateQueueSender(options);
        
        // Initialize governance validator with config file
        _governanceValidator = new RuntimeGovernanceValidator(
            isEnabled: () => true,
            profileName: options.GovernanceProfile,
            source: new FileGovernanceSource(
                options.GovernanceConfigPath ?? "cerbi_governance.json"
            )
        );
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < _options.RestrictedToMinimumLevel)
            return;

        // Extract properties for governance validation
        var properties = logEvent.Properties
            .ToDictionary(k => k.Key, k => GetPropertyValue(k.Value));

        // Run governance validation - severities come from config
        var violations = new List<ViolationDto>();
        var result = _governanceValidator.Validate(properties);
        violations = result.Violations.Select(v => new ViolationDto
        {
            RuleId = v.RuleId,
            Code = v.Code,
            Field = v.Field,
            Severity = v.Severity,  // FROM GOVERNANCE CONFIG
            Message = v.Message
        }).ToList();

        // Build DTO - Score is null, Scoring API computes it
        var scoringEvent = new ScoringEventDto
        {
            SchemaVersion = ContractVersions.ScoringEventSchemaVersion,
            TenantId = GetProperty(logEvent, "TenantId", _options.TenantId),
            AppName = GetProperty(logEvent, "Application", _options.ApplicationName),
            Environment = GetProperty(logEvent, "Environment", _options.Environment),
            Runtime = "dotnet",
            LogId = Guid.NewGuid().ToString(),
            CorrelationId = GetProperty(logEvent, "CorrelationId", null),
            TimestampUtc = logEvent.Timestamp.UtcDateTime,
            GovernanceProfile = _options.GovernanceProfile,
            LogLevel = MapLogLevel(logEvent.Level),
            
            Score = null,  // Scoring API computes this
            
            Violations = violations,
            GovernanceFlags = new GovernanceFlagsDto
            {
                GovernanceRelaxed = GetBoolProperty(logEvent, "GovernanceRelaxed")
            },
            RawPayload = BuildRawPayload(logEvent)
        };

        // Send to queue
        var json = JsonSerializer.Serialize(scoringEvent);
        _ = _queueSender.SendAsync(json, scoringEvent.LogId);
    }

    private static string MapLogLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "Trace",
        LogEventLevel.Debug => "Debug",
        LogEventLevel.Information => "Information",
        LogEventLevel.Warning => "Warning",
        LogEventLevel.Error => "Error",
        LogEventLevel.Fatal => "Critical",
        _ => "Information"
    };

    private static string? GetProperty(LogEvent evt, string key, string? defaultValue)
    {
        if (evt.Properties.TryGetValue(key, out var prop) && prop is ScalarValue sv)
            return sv.Value?.ToString();
        return defaultValue;
    }

    private static bool GetBoolProperty(LogEvent evt, string key)
    {
        if (evt.Properties.TryGetValue(key, out var prop) && prop is ScalarValue sv)
        {
            if (sv.Value is bool b) return b;
            if (bool.TryParse(sv.Value?.ToString(), out var parsed)) return parsed;
        }
        return false;
    }

    private static object? GetPropertyValue(LogEventPropertyValue value) => value switch
    {
        ScalarValue sv => sv.Value,
        SequenceValue seq => seq.Elements.Select(GetPropertyValue).ToList(),
        StructureValue str => str.Properties.ToDictionary(p => p.Name, p => GetPropertyValue(p.Value)),
        _ => value.ToString()
    };

    private static object BuildRawPayload(LogEvent logEvent)
    {
        var payload = new Dictionary<string, object?>
        {
            ["MessageTemplate"] = logEvent.MessageTemplate.Text,
            ["RenderedMessage"] = logEvent.RenderMessage(),
            ["Level"] = logEvent.Level.ToString()
        };

        if (logEvent.Exception != null)
        {
            payload["Exception"] = new
            {
                Type = logEvent.Exception.GetType().FullName,
                Message = logEvent.Exception.Message
            };
        }

        foreach (var prop in logEvent.Properties)
            payload[prop.Key] = GetPropertyValue(prop.Value);

        return payload;
    }

    private static IQueueSender CreateQueueSender(CerbiStreamSinkOptions options)
    {
        return options.QueueType.ToLowerInvariant() switch
        {
            "azureservicebus" => new AzureServiceBusQueueSender(
                options.QueueConnectionString, options.QueueName),
            _ => throw new ArgumentException($"Unknown queue type: {options.QueueType}")
        };
    }

    public void Dispose() => (_queueSender as IDisposable)?.Dispose();
}
```

### 4. Extension Method

**File:** `LoggerConfigurationExtensions.cs`

```csharp
using Serilog;
using Serilog.Configuration;

namespace CerbiStream.Sinks.Serilog;

public static class LoggerConfigurationExtensions
{
    public static LoggerConfiguration CerbiStream(
        this LoggerSinkConfiguration sinkConfiguration,
        Action<CerbiStreamSinkOptions> configure)
    {
        var options = new CerbiStreamSinkOptions();
        configure(options);
        return sinkConfiguration.Sink(new CerbiStreamSink(options));
    }
}
```

---

## Usage

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.CerbiStream(options =>
    {
        options.TenantId = "my-tenant";
        options.ApplicationName = "MyApp";
        options.Environment = "Production";
        options.GovernanceProfile = "default";
        options.QueueConnectionString = config["ServiceBus:ConnectionString"];
    })
    .CreateLogger();
```

**Developer does NOT configure:**
- Severity weights
- Score deductions
- Rule definitions

All scoring rules are in `cerbi_governance.json` and computed by Scoring API.

---

## Testing

Verify:
1. `Score` field is `null` in output
2. `Severity` on violations matches governance config
3. Violations extracted correctly from Serilog properties
