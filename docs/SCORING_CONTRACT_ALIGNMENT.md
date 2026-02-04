# CerbiStream → Scoring API Contract Alignment

**Date:** 2026-02-03  
**Status:** 🔴 Action Required  
**Priority:** High

---

## Problem Statement

CerbiStream SDK and the Scoring API have **mismatched message formats**, causing messages sent to the Service Bus queue to be rejected or ignored by the Scoring API.

### Current CerbiStream Output Format
```json
{
  "LogId": "guid-here",
  "LogData": {
    "ApplicationMessage": "User logged in",
    "LogLevel": "Information",
    "GovernanceViolations": [...],
    "GovernanceRelaxed": false,
    ...metadata...
  }
}
```

### Expected Scoring API Format (`ScoringEventDto`)
```json
{
  "SchemaVersion": "1.0",
  "TenantId": "tenant-123",
  "AppName": "MyApp",
  "Environment": "Production",
  "Runtime": "dotnet",
  "LogId": "guid-here",
  "CorrelationId": "correlation-guid",
  "TimestampUtc": "2026-02-03T22:00:00Z",
  "GovernanceProfile": "default",
  "GovernanceMode": "Strict",
  "LogLevel": "Information",
  "Score": {
    "Overall": 80,
    "Governance": 70,
    "Safety": 75
  },
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
  "RawPayload": { ...original log data... }
}
```

---

## Required Changes

### 1. CerbiStream SDK Changes

#### File: `LoggingStandards/Classes/Logging.cs`

Update `SendLogAsync()` to transform log entries into `ScoringEventDto` format:

```csharp
private async Task<bool> SendLogAsync(object logEntry)
{
    var logId = Guid.NewGuid().ToString();
    
    // Transform to ScoringEventDto format
    var scoringEvent = TransformToScoringEvent(logEntry, logId);
    
    string payload = _jsonConverter.ConvertMessageToJson(scoringEvent);
    
    // ... rest of existing logic (encryption, queue send, etc.)
}

private ScoringEventDto TransformToScoringEvent(object logEntry, string logId)
{
    var data = ExtractLogData(logEntry);
    var violations = ExtractViolations(data);
    var score = ComputeScore(violations);
    
    return new ScoringEventDto
    {
        SchemaVersion = "1.0",
        TenantId = _options.TenantId ?? "unknown",
        AppName = _options.ApplicationName ?? "unknown",
        Environment = _options.Environment ?? "unknown",
        Runtime = "dotnet",
        LogId = logId,
        CorrelationId = ExtractCorrelationId(data),
        TimestampUtc = DateTime.UtcNow,
        GovernanceProfile = _options.GovernanceProfile ?? "default",
        GovernanceMode = _options.GovernanceMode ?? "Strict",
        LogLevel = ExtractLogLevel(data),
        Score = score,
        Violations = violations,
        GovernanceFlags = new GovernanceFlagsDto
        {
            GovernanceRelaxed = ExtractGovernanceRelaxed(data)
        },
        RawPayload = data
    };
}
```

#### File: `LoggingStandards/Configuration/CerbiStreamOptions.cs`

Add required properties:

```csharp
public string? TenantId { get; private set; }
public string? GovernanceMode { get; private set; } = "Strict";

public CerbiStreamOptions WithTenantId(string tenantId)
{
    TenantId = tenantId;
    return this;
}

public CerbiStreamOptions WithGovernanceMode(string mode)
{
    GovernanceMode = mode;
    return this;
}
```

### 2. New Contract Types

#### File: `LoggingStandards/Contracts/ScoringEventDto.cs`

```csharp
namespace CerbiStream.Contracts;

public class ScoringEventDto
{
    public string SchemaVersion { get; set; } = "1.0";
    public string TenantId { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string? Environment { get; set; }
    public string? Runtime { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? GovernanceProfile { get; set; }
    public string? GovernanceMode { get; set; }
    public string? LogLevel { get; set; }
    public ScoreBreakdownDto? Score { get; set; }
    public List<ViolationDto>? Violations { get; set; }
    public GovernanceFlagsDto? GovernanceFlags { get; set; }
    public object? RawPayload { get; set; }
}

public class ScoreBreakdownDto
{
    public int Overall { get; set; }
    public int Governance { get; set; }
    public int Safety { get; set; }
}

public class ViolationDto
{
    public string? RuleId { get; set; }
    public string? Code { get; set; }
    public string? Field { get; set; }
    public string? Severity { get; set; }
    public string? Message { get; set; }
}

public class GovernanceFlagsDto
{
    public bool GovernanceRelaxed { get; set; }
}
```

### 3. Score Computation Logic

#### File: `LoggingStandards/Services/ScoringCalculator.cs`

```csharp
namespace CerbiStream.Services;

public static class ScoringCalculator
{
    public static ScoreBreakdownDto ComputeScore(List<ViolationDto>? violations)
    {
        if (violations == null || violations.Count == 0)
        {
            return new ScoreBreakdownDto { Overall = 100, Governance = 100, Safety = 100 };
        }

        int governanceDeduction = 0;
        int safetyDeduction = 0;

        foreach (var v in violations)
        {
            var deduction = v.Severity?.ToLower() switch
            {
                "critical" => 25,
                "error" => 15,
                "warning" => 5,
                "info" => 1,
                _ => 5
            };

            // PII/Security violations affect safety score
            if (v.Code?.Contains("PII", StringComparison.OrdinalIgnoreCase) == true ||
                v.Code?.Contains("Security", StringComparison.OrdinalIgnoreCase) == true)
            {
                safetyDeduction += deduction;
            }
            else
            {
                governanceDeduction += deduction;
            }
        }

        var governance = Math.Max(0, 100 - governanceDeduction);
        var safety = Math.Max(0, 100 - safetyDeduction);
        var overall = (governance + safety) / 2;

        return new ScoreBreakdownDto
        {
            Overall = overall,
            Governance = governance,
            Safety = safety
        };
    }
}
```

---

## Migration Path

### Phase 1: Add Contract Types (Non-Breaking)
1. Add `ScoringEventDto` and related DTOs to CerbiStream
2. Add `TenantId`, `GovernanceMode` to `CerbiStreamOptions`
3. Unit tests for new types

### Phase 2: Transform Output (Breaking Change)
1. Update `Logging.SendLogAsync()` to produce `ScoringEventDto`
2. Add `ScoringCalculator` for score computation
3. Integration tests with Scoring API

### Phase 3: Update All Queue Providers
- No changes needed! All providers use `Logging.SendLogAsync()`
- The transform happens before queue send

---

## Configuration Example

```csharp
builder.Logging.AddCerbiStream(options => options
    .ForProduction()
    .WithTenantId("my-tenant-id")
    .WithApplicationIdentity("MyApp", "Production")
    .WithGovernanceProfile("default")
    .WithGovernanceMode("Strict")
    .WithQueue("AzureServiceBus", connectionString, queueName)
);
```

---

## Verification

After implementing, verify with:

```bash
# 1. Run test app
cd RealisiticLoggingTesting
dotnet run

# 2. Check Service Bus queue
az servicebus queue show -g rg-cerbi-dev --namespace-name cerbi-dev-sbus \
  --name cerbishield.log-scoring --query "countDetails.activeMessageCount"

# 3. Check Scoring API logs for successful processing
az containerapp logs show -g rg-cerbi-dev -n cerbi-dev-scoring-api --tail 50

# 4. Check database for events
SELECT COUNT(*) FROM scoring_events;
SELECT * FROM scoring_events ORDER BY created_at DESC LIMIT 5;
```

---

## Related Documents

- [CerbiStream Scoring Integration](./CERBISTREAM_SCORING_INTEGRATION.md)
- [Scoring API Contract Spec](../../CerbiShield.ScoringApi/docs/CONTRACT.md)
- [Message Format Spec](./teams/CERBISTREAM_MESSAGE_FORMAT.md)
