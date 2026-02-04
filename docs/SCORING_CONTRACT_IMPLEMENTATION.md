# CerbiStream SDK - Scoring Contract Integration

**Repository:** `Cerbi-CerbiStream`  
**Date:** 2026-02-03  
**Status:** ?? Implementation Required

---

## Overview

CerbiStream SDK intercepts log messages, applies governance validation using the governance config file, and sends events to the Scoring API. 

**Critical:** The SDK does **NOT** compute scores. It sends violations (with severities from governance config) and the Scoring API computes scores centrally using the governance configuration.

---

## Architecture

```
???????????????????????????????????????????????????????????????????????????
?                           CerbiStream SDK                                ?
?                                                                          ?
?   Log Entry ? Governance Validator ? Extract Violations ? Send to Queue ?
?                      ?                                                   ?
?                      ?                                                   ?
?            cerbi_governance.json                                         ?
?            (rules & severities defined here)                             ?
????????????????????????????????????????????????????????????????????????????
                                    ?
                                    ? ScoringEventDto (violations only, NO score)
                                    ?
????????????????????????????????????????????????????????????????????????????
?                           Scoring API                                     ?
?                                                                           ?
?   Receive Event ? Read Governance Config ? Compute Scores ? Store        ?
?                           ?                                               ?
?                           ?                                               ?
?                 Score Calculation (centralized)                           ?
?                 - Reads severity weights from governance config           ?
?                 - Applies tenant-specific rules                           ?
?                 - Consistent scoring across all SDKs                      ?
????????????????????????????????????????????????????????????????????????????
```

**Key Principle:** 
- Developers don't configure scoring rules
- Governance config maintains ALL rules and severities
- Scoring API computes scores centrally
- All SDKs produce identical output format

---

## What CerbiStream Sends

### ScoringEventDto (Score field is NULL - computed by Scoring API)

```json
{
  "SchemaVersion": "1.0",
  "TenantId": "my-tenant",
  "AppName": "MyApp",
  "LogId": "guid",
  "TimestampUtc": "2026-02-03T22:00:00Z",
  "GovernanceProfile": "default",
  "LogLevel": "Information",
  "Score": null,
  "Violations": [
    {
      "RuleId": "PII-001",
      "Code": "PII",
      "Field": "user.email",
      "Severity": "Warning",
      "Message": "PII field detected"
    }
  ],
  "GovernanceFlags": {
    "GovernanceRelaxed": false
  },
  "RawPayload": { ... }
}
```

**Important:** 
- `Score` is `null` - Scoring API computes it
- `Severity` on violations comes from governance config, not hardcoded

---

## Changes Required

### Change 1: Add Package Reference

**File:** `LoggingStandards/CerbiStream.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Cerbi.Contracts" Version="1.1.0" />
</ItemGroup>
```

---

### Change 2: Update CerbiStreamOptions

**File:** `LoggingStandards/Configuration/CerbiStreamOptions.cs`

**Add only what's needed for routing (no scoring config):**

```csharp
/// <summary>
/// Tenant identifier for multi-tenant support. Required.
/// </summary>
public string? TenantId { get; private set; }

public CerbiStreamOptions WithTenantId(string tenantId)
{
    TenantId = tenantId;
    return this;
}
```

---

### Change 3: Create ScoringEventTransformer

**File:** `LoggingStandards/Services/ScoringEventTransformer.cs`

```csharp
using Cerbi.Contracts;
using Cerbi.Contracts.Scoring;
using CerbiStream.Logging.Configuration;
using System;
using System.Collections.Generic;

namespace CerbiStream.Services;

/// <summary>
/// Transforms log entries to ScoringEventDto format.
/// 
/// IMPORTANT: This transformer does NOT compute scores.
/// - Violations are extracted from GovernanceRuntimeAdapter results
/// - Severities come from cerbi_governance.json via the validator
/// - Score computation happens in the Scoring API
/// </summary>
public static class ScoringEventTransformer
{
    public static ScoringEventDto Transform(
        object logEntry,
        string logId,
        CerbiStreamOptions options,
        IDictionary<string, object>? enrichedData = null)
    {
        var data = ExtractAsDictionary(logEntry, enrichedData);
        var violations = ExtractViolations(data);

        return new ScoringEventDto
        {
            SchemaVersion = ContractVersions.ScoringEventSchemaVersion,
            TenantId = options.TenantId ?? ExtractString(data, "TenantId") ?? "unknown",
            AppName = options.ApplicationName ?? ExtractString(data, "ApplicationName") ?? "unknown",
            Environment = options.Environment ?? ExtractString(data, "Environment"),
            Runtime = "dotnet",
            LogId = logId,
            CorrelationId = ExtractString(data, "CorrelationId") ?? ExtractString(data, "RequestId"),
            TimestampUtc = DateTime.UtcNow,
            GovernanceProfile = options.GovernanceProfile 
                ?? ExtractString(data, "GovernanceProfileUsed") 
                ?? "default",
            GovernanceMode = ExtractString(data, "GovernanceMode") ?? "Strict",
            LogLevel = ExtractString(data, "LogLevel") ?? "Information",
            
            // Score is NULL - Scoring API computes it using governance config
            Score = null,
            
            // Violations with severities from governance config
            Violations = violations,
            
            GovernanceFlags = new GovernanceFlagsDto
            {
                GovernanceRelaxed = ExtractBool(data, "GovernanceRelaxed")
            },
            RawPayload = data
        };
    }

    /// <summary>
    /// Extracts violations from GovernanceRuntimeAdapter results.
    /// Severities are already set by the governance validator from cerbi_governance.json.
    /// </summary>
    private static List<ViolationDto> ExtractViolations(IDictionary<string, object> data)
    {
        var result = new List<ViolationDto>();

        if (!data.TryGetValue("GovernanceViolations", out var rawViolations) || rawViolations == null)
            return result;

        if (rawViolations is IEnumerable<object> enumerable)
        {
            foreach (var item in enumerable)
            {
                var violation = MapToViolationDto(item);
                if (violation != null)
                    result.Add(violation);
            }
        }

        return result;
    }

    private static ViolationDto? MapToViolationDto(object item)
    {
        // Handle dictionary format
        if (item is IDictionary<string, object> dict)
        {
            return new ViolationDto
            {
                RuleId = ExtractString(dict, "RuleId"),
                Code = ExtractString(dict, "Code"),
                Field = ExtractString(dict, "Field"),
                // Severity from governance config - NOT hardcoded
                Severity = ExtractString(dict, "Severity"),
                Message = ExtractString(dict, "Message")
            };
        }

        // Handle Cerbi.Governance.GovernanceViolation type via reflection
        var type = item.GetType();
        if (type.Name == "GovernanceViolation")
        {
            return new ViolationDto
            {
                RuleId = GetPropertyValue(item, "RuleId"),
                Code = GetPropertyValue(item, "Code"),
                Field = GetPropertyValue(item, "Field"),
                Severity = GetPropertyValue(item, "Severity"),
                Message = GetPropertyValue(item, "Message")
            };
        }

        return null;
    }

    private static string? GetPropertyValue(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj)?.ToString();
    }

    private static IDictionary<string, object> ExtractAsDictionary(
        object logEntry, 
        IDictionary<string, object>? enrichedData)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (enrichedData != null)
        {
            foreach (var kvp in enrichedData)
                result[kvp.Key] = kvp.Value;
        }

        if (logEntry is IDictionary<string, object> dict)
        {
            foreach (var kvp in dict)
                result[kvp.Key] = kvp.Value;
        }
        else
        {
            foreach (var prop in logEntry.GetType().GetProperties())
            {
                try
                {
                    var value = prop.GetValue(logEntry);
                    if (value != null)
                        result[prop.Name] = value;
                }
                catch { }
            }
        }

        return result;
    }

    private static string? ExtractString(IDictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static bool ExtractBool(IDictionary<string, object> data, string key)
    {
        if (data.TryGetValue(key, out var value))
        {
            if (value is bool b) return b;
            if (bool.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return false;
    }
}
```

---

### Change 4: Update Logging.cs

**File:** `LoggingStandards/Classes/Logging.cs`

**Add using:**
```csharp
using Cerbi.Contracts.Scoring;
using CerbiStream.Services;
```

**Update SendLogAsync:**
```csharp
private async Task<bool> SendLogAsync(object logEntry, IDictionary<string, object>? enrichedData = null)
{
    try
    {
        var logId = Guid.NewGuid().ToString();

        // Transform to ScoringEventDto (score is null - API computes it)
        var scoringEvent = ScoringEventTransformer.Transform(
            logEntry, 
            logId, 
            _options,
            enrichedData);

        string payload = _jsonConverter.ConvertMessageToJson(scoringEvent);

        if (_options.EncryptionMode != IEncryptionTypeProvider.EncryptionType.None && _encryption.IsEnabled)
        {
            payload = _encryption.Encrypt(payload);
            Console.WriteLine($"[CerbiStream] Payload for log ID {logId} encrypted ({_options.EncryptionMode}).");
        }

        Console.WriteLine($"[CerbiStream] Sending log ID {logId}...");

        if (_options.DisableQueueSending)
        {
            Console.WriteLine($"[CerbiStream] Queue send disabled; log ID {logId} dropped.");
            return true;
        }

        if (_options.EnableQueueRetries)
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    _options.QueueRetryCount,
                    idx => TimeSpan.FromMilliseconds(_options.QueueRetryDelayMilliseconds),
                    (ex, span, retry, ctx) =>
                    {
                        Console.WriteLine($"[CerbiStream] Retry {retry} failed for log ID {logId}. Error: {ex.Message}");
                    });

            var sentWithRetry = await policy.ExecuteAsync(() => _queue.SendMessageAsync(payload, logId));
            return sentWithRetry;
        }

        var sent = await _queue.SendMessageAsync(payload, logId);
        return sent;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CerbiStream] Logging failed. Error: {ex.Message}");
        return false;
    }
}
```

---

## Developer Configuration

### What Developers Set

```csharp
builder.Logging.AddCerbiStream(options => options
    .ForProduction()
    .WithTenantId("my-tenant-123")            // Required
    .WithApplicationIdentity("MyApp", "Production")
    .WithGovernanceProfile("default")          // Which profile from config
    .WithQueue("AzureServiceBus", connectionString, queueName)
);
```

### What Developers DON'T Set

| ? Not Configurable in SDK | ? Defined In |
|---------------------------|---------------|
| Severity weights | `cerbi_governance.json` |
| Score deductions | Scoring API config |
| Rule definitions | `cerbi_governance.json` |
| Field validations | `cerbi_governance.json` |
| Category mappings | Governance config |

---

## Governance Config (cerbi_governance.json)

All rules and severities are defined here:

```json
{
  "profiles": {
    "default": {
      "rules": [
        {
          "ruleId": "PII-001",
          "code": "PII",
          "fields": ["email", "ssn", "phone"],
          "severity": "Warning",
          "action": "Redact"
        },
        {
          "ruleId": "SEC-001",
          "code": "Security", 
          "fields": ["password", "apiKey"],
          "severity": "Critical",
          "action": "Block"
        }
      ]
    }
  }
}
```

The SDK's `GovernanceRuntimeAdapter` reads this config and tags violations with the configured severity.

---

## File Summary

| File | Action | Description |
|------|--------|-------------|
| `CerbiStream.csproj` | MODIFY | Add Cerbi.Contracts reference |
| `CerbiStreamOptions.cs` | MODIFY | Add TenantId only |
| `Services/ScoringEventTransformer.cs` | CREATE | Transform to DTO, NO score |
| `Classes/Logging.cs` | MODIFY | Use transformer |

---

## Testing

```bash
cd Cerbi-CerbiStream
dotnet restore
dotnet build
dotnet test
```

Verify output has `Score: null` and violations have severities from governance config.
