# CerbiStream Scoring Integration Update

**Date:** 2026-02-03  
**Version:** 2.1.0  
**Status:** 🔴 Breaking Change Required

---

## Summary

CerbiStream SDK must be updated to output `ScoringEventDto` format for compatibility with CerbiShield Scoring API.

---

## Current State

```
CerbiStream → { LogId, LogData } → Queue → ❌ Scoring API rejects
```

## Target State

```
CerbiStream → { ScoringEventDto } → Queue → ✅ Scoring API processes
```

---

## Required Changes

### 1. Add NuGet Reference

```xml
<PackageReference Include="Cerbi.Contracts" Version="1.0.0" />
```

### 2. Update `CerbiStreamOptions.cs`

Add new configuration properties:

```csharp
public string? TenantId { get; private set; }
public string? GovernanceMode { get; private set; } = "Strict";

public CerbiStreamOptions WithTenantId(string tenantId)
{
    TenantId = tenantId;
    return this;
}
```

### 3. Update `Logging.cs`

Transform output to `ScoringEventDto`:

```csharp
private ScoringEventDto TransformToScoringEvent(object logEntry, string logId)
{
    // Extract data, compute scores, format violations
    // See SCORING_CONTRACT_ALIGNMENT.md for full implementation
}
```

### 4. Add Score Calculator

New file `Services/ScoringCalculator.cs`:
- Compute scores based on violation count/severity
- Return `ScoreBreakdownDto`

---

## Configuration Migration

### Before (v2.0)
```csharp
builder.Logging.AddCerbiStream(o => o
    .ForProduction()
    .WithApplicationIdentity("MyApp", "Production")
    .WithQueue("AzureServiceBus", conn, queue)
);
```

### After (v2.1)
```csharp
builder.Logging.AddCerbiStream(o => o
    .ForProduction()
    .WithTenantId("my-tenant-123")           // NEW
    .WithApplicationIdentity("MyApp", "Production")
    .WithGovernanceMode("Strict")             // NEW (optional)
    .WithQueue("AzureServiceBus", conn, queue)
);
```

---

## Breaking Changes

| Change | Impact | Migration |
|--------|--------|-----------|
| Message format | High | Scoring API v2+ required |
| TenantId required | Medium | Add to config |
| Score computed client-side | Low | Automatic |

---

## Testing

```bash
# Run integration tests
dotnet test --filter "Category=ScoringIntegration"

# Verify message format
dotnet run --project TestQueueSender -- --validate-schema
```

---

## Files Modified

```
LoggingStandards/
├── Classes/
│   └── Logging.cs                    # Transform logic
├── Configuration/
│   └── CerbiStreamOptions.cs         # New properties
├── Contracts/                        # NEW (or use Cerbi.Contracts)
│   └── ScoringEventDto.cs
└── Services/
    └── ScoringCalculator.cs          # NEW
```

---

## Related Docs

- [Scoring Contract Alignment](./SCORING_CONTRACT_ALIGNMENT.md)
- [Plugin Update Checklist](./PLUGIN_UPDATE_CHECKLIST.md)
- [Message Format Spec](./SCORING_EVENT_MESSAGE_FORMAT.md)
