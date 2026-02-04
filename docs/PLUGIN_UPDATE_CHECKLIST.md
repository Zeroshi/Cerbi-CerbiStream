# CerbiStream Plugin Update Checklist

**Date:** 2026-02-03  
**Purpose:** Track all queue plugins that need contract alignment updates

---

## Overview

All CerbiStream queue plugins share a common `ISendMessage` interface and use the central `Logging.cs` class for message transformation. The contract alignment fix should be implemented **once** in `Logging.cs`, automatically affecting all plugins.

---

## Queue Plugins Inventory

| Plugin | File | Status | Notes |
|--------|------|--------|-------|
| Azure Service Bus | `Classes/Queues/AzureServiceBus.cs` | 🟡 Pending | Primary production queue |
| RabbitMQ | `Classes/Queues/RabbitMQ.cs` | 🟡 Pending | On-prem option |
| Kafka | `Classes/Queues/Kafka.cs` | 🟡 Pending | High-throughput option |
| Azure Storage Queue | `Classes/Queues/AzureStorageQueue.cs` | 🟡 Pending | Budget option |
| AWS SQS | `Classes/Queues/AwsSqs.cs` | 🟡 Pending | AWS customers |
| GCP Pub/Sub | `Classes/Queues/GcpPubSub.cs` | 🟡 Pending | GCP customers |
| In-Memory (Test) | `Classes/Queues/InMemoryQueue.cs` | 🟡 Pending | Unit testing |

---

## Single Point of Change

### ✅ Good News

All plugins implement `ISendMessage`:

```csharp
public interface ISendMessage
{
    Task<bool> SendMessageAsync(string payload, string logId);
}
```

The `payload` string is created in `Logging.cs`:

```
LogEntry → Logging.SendLogAsync() → JSON Transform → ISendMessage.SendMessageAsync()
```

**This means we only need to update `Logging.cs` to change the JSON format for ALL plugins!**

---

## Implementation Checklist

### Core Changes (One-Time)

- [ ] **`Logging.cs`** - Transform to `ScoringEventDto` format
- [ ] **`CerbiStreamOptions.cs`** - Add `TenantId`, `GovernanceMode` properties
- [ ] **`Contracts/ScoringEventDto.cs`** - New file with DTO classes
- [ ] **`Services/ScoringCalculator.cs`** - Score computation logic

### Plugin Verification (No Code Changes Expected)

- [ ] **AzureServiceBus** - Verify sends correctly formatted JSON
- [ ] **RabbitMQ** - Verify sends correctly formatted JSON
- [ ] **Kafka** - Verify sends correctly formatted JSON
- [ ] **Azure Storage Queue** - Verify sends correctly formatted JSON
- [ ] **AWS SQS** - Verify sends correctly formatted JSON
- [ ] **GCP Pub/Sub** - Verify sends correctly formatted JSON
- [ ] **In-Memory** - Update test assertions for new format

### Test Updates

- [ ] **Unit Tests** - Update expected JSON format in mocks
- [ ] **Integration Tests** - Verify end-to-end with Scoring API
- [ ] **Contract Tests** - Add schema validation tests

---

## Plugin-Specific Considerations

### Azure Service Bus
```csharp
// No changes needed - just receives string payload
public async Task<bool> SendMessageAsync(string message, string messageId)
{
    var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(message))
    {
        ApplicationProperties = { [nameof(messageId)] = messageId.ToString() }
    };
    await _sender.SendMessageAsync(serviceBusMessage);
    return true;
}
```

### Kafka
```csharp
// May need to set message key for partitioning
// Consider using TenantId or AppName as partition key
```

### AWS SQS
```csharp
// Check message size limits (256KB)
// May need MessageGroupId for FIFO queues
```

---

## Rollout Strategy

### Phase 1: Development (Week 1)
1. Implement `ScoringEventDto` contracts
2. Update `Logging.cs` with transform logic
3. Add feature flag for gradual rollout
4. Unit test all changes

### Phase 2: Testing (Week 2)
1. Deploy to dev environment
2. Verify Scoring API processes messages
3. Check dashboard displays data
4. Load test with realistic traffic

### Phase 3: Production (Week 3)
1. Enable feature flag for 10% of traffic
2. Monitor for errors/dead letters
3. Gradually increase to 100%
4. Remove feature flag

---

## Feature Flag Implementation

```csharp
// CerbiStreamOptions.cs
public bool UseScoringEventFormat { get; private set; } = true;

public CerbiStreamOptions WithLegacyFormat()
{
    UseScoringEventFormat = false;
    return this;
}

// Logging.cs
private async Task<bool> SendLogAsync(object logEntry)
{
    var logId = Guid.NewGuid().ToString();
    
    object payload = _options.UseScoringEventFormat
        ? TransformToScoringEvent(logEntry, logId)
        : new { LogId = logId, LogData = logEntry };  // Legacy format
    
    string json = _jsonConverter.ConvertMessageToJson(payload);
    // ...
}
```

---

## Success Criteria

1. ✅ Messages appear in Service Bus queue
2. ✅ Scoring API processes without dead letters
3. ✅ Events written to `scoring_events` table
4. ✅ Aggregator processes events
5. ✅ Dashboard displays governance scores

---

## Related Files

```
Cerbi-CerbiStream/
├── LoggingStandards/
│   ├── Classes/
│   │   ├── Logging.cs                    # ← PRIMARY CHANGE
│   │   └── Queues/
│   │       ├── AzureServiceBus.cs        # No change
│   │       ├── RabbitMQ.cs               # No change
│   │       ├── Kafka.cs                  # No change
│   │       └── ...
│   ├── Configuration/
│   │   └── CerbiStreamOptions.cs         # ← Add properties
│   ├── Contracts/                        # ← NEW FOLDER
│   │   └── ScoringEventDto.cs            # ← NEW FILE
│   └── Services/                         # ← NEW FOLDER
│       └── ScoringCalculator.cs          # ← NEW FILE
└── docs/
    ├── SCORING_CONTRACT_ALIGNMENT.md
    └── PLUGIN_UPDATE_CHECKLIST.md        # This file
```
