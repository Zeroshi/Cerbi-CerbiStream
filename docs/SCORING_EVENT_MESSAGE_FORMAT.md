# Scoring Event Message Format Specification

**Version:** 1.0  
**Date:** 2026-02-03  
**Status:** Draft

---

## Overview

This document defines the canonical message format for scoring events sent from CerbiStream SDK to the CerbiShield Scoring API via message queues (Service Bus, Kafka, RabbitMQ, etc.).

---

## Message Schema

### Root Object: `ScoringEventDto`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `SchemaVersion` | string | ✅ | Must be `"1.0"` |
| `TenantId` | string | ✅ | Unique tenant identifier |
| `AppName` | string | ✅ | Application name |
| `Environment` | string | ❌ | Deployment environment (dev/staging/prod) |
| `Runtime` | string | ❌ | Runtime platform (dotnet/java/node/python) |
| `LogId` | string | ✅ | Unique log entry identifier (UUID) |
| `CorrelationId` | string | ❌ | Request correlation ID for tracing |
| `TimestampUtc` | datetime | ✅ | ISO 8601 UTC timestamp |
| `GovernanceProfile` | string | ❌ | Active governance profile name |
| `GovernanceMode` | string | ❌ | Governance mode (Strict/Relaxed/Monitor) |
| `LogLevel` | string | ❌ | Log severity level |
| `Score` | ScoreBreakdown | ❌ | Computed scores |
| `Violations` | Violation[] | ❌ | List of governance violations |
| `GovernanceFlags` | GovernanceFlags | ❌ | Governance state flags |
| `RawPayload` | object | ❌ | Original log data (for debugging) |

### Nested: `ScoreBreakdown`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Overall` | int | ✅ | Overall score (0-100) |
| `Governance` | int | ✅ | Governance compliance score (0-100) |
| `Safety` | int | ✅ | Data safety score (0-100) |

### Nested: `Violation`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `RuleId` | string | ❌ | Unique rule identifier |
| `Code` | string | ❌ | Violation category code |
| `Field` | string | ❌ | Field that caused violation |
| `Severity` | string | ❌ | Severity level (Critical/Error/Warning/Info) |
| `Message` | string | ❌ | Human-readable description |

### Nested: `GovernanceFlags`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `GovernanceRelaxed` | bool | ✅ | Whether governance was bypassed |

---

## Example Messages

### Clean Log (No Violations)

```json
{
  "SchemaVersion": "1.0",
  "TenantId": "acme-corp",
  "AppName": "OrderService",
  "Environment": "Production",
  "Runtime": "dotnet",
  "LogId": "550e8400-e29b-41d4-a716-446655440000",
  "CorrelationId": "req-12345",
  "TimestampUtc": "2026-02-03T22:30:00.000Z",
  "GovernanceProfile": "default",
  "GovernanceMode": "Strict",
  "LogLevel": "Information",
  "Score": {
    "Overall": 100,
    "Governance": 100,
    "Safety": 100
  },
  "Violations": [],
  "GovernanceFlags": {
    "GovernanceRelaxed": false
  },
  "RawPayload": {
    "Message": "Order processed successfully",
    "OrderId": "ORD-123",
    "Amount": 99.99
  }
}
```

### Log with Violations

```json
{
  "SchemaVersion": "1.0",
  "TenantId": "acme-corp",
  "AppName": "UserService",
  "Environment": "Production",
  "Runtime": "dotnet",
  "LogId": "550e8400-e29b-41d4-a716-446655440001",
  "CorrelationId": "req-67890",
  "TimestampUtc": "2026-02-03T22:31:00.000Z",
  "GovernanceProfile": "strict-pii",
  "GovernanceMode": "Strict",
  "LogLevel": "Warning",
  "Score": {
    "Overall": 65,
    "Governance": 80,
    "Safety": 50
  },
  "Violations": [
    {
      "RuleId": "PII-001",
      "Code": "PII",
      "Field": "user.email",
      "Severity": "Warning",
      "Message": "Email address detected in log payload"
    },
    {
      "RuleId": "PII-002",
      "Code": "PII",
      "Field": "user.phone",
      "Severity": "Error",
      "Message": "Phone number detected in log payload"
    }
  ],
  "GovernanceFlags": {
    "GovernanceRelaxed": false
  },
  "RawPayload": {
    "Message": "User login attempt",
    "user": {
      "email": "[REDACTED]",
      "phone": "[REDACTED]"
    }
  }
}
```

### Relaxed Governance Log

```json
{
  "SchemaVersion": "1.0",
  "TenantId": "acme-corp",
  "AppName": "DebugService",
  "Environment": "Development",
  "Runtime": "dotnet",
  "LogId": "550e8400-e29b-41d4-a716-446655440002",
  "TimestampUtc": "2026-02-03T22:32:00.000Z",
  "GovernanceProfile": "default",
  "GovernanceMode": "Relaxed",
  "LogLevel": "Debug",
  "Score": {
    "Overall": 100,
    "Governance": 100,
    "Safety": 100
  },
  "Violations": [],
  "GovernanceFlags": {
    "GovernanceRelaxed": true
  },
  "RawPayload": {
    "Message": "Debug info",
    "SensitiveData": "allowed-in-relaxed-mode"
  }
}
```

---

## Scoring Algorithm

### Base Score
All logs start with a score of 100 in each category.

### Deductions by Severity

| Severity | Governance Deduction | Safety Deduction |
|----------|---------------------|------------------|
| Critical | 25 | 25 |
| Error | 15 | 15 |
| Warning | 5 | 5 |
| Info | 1 | 1 |

### Category Assignment

- **Safety Score** affected by: PII, Security, Encryption violations
- **Governance Score** affected by: Policy, Schema, Format violations

### Overall Score
```
Overall = (Governance + Safety) / 2
```

### Minimum Score
All scores have a floor of 0.

---

## Envelope Format (Optional)

For systems requiring additional routing metadata, wrap in an envelope:

```json
{
  "EnvelopeVersion": "1.0",
  "IdempotencyKey": "550e8400-e29b-41d4-a716-446655440000",
  "SourceSystem": "CerbiStream",
  "SourceVersion": "2.1.0",
  "Payload": {
    // ScoringEventDto here
  }
}
```

---

## Validation Rules

1. **SchemaVersion** must equal `"1.0"`
2. **TenantId** must be non-empty
3. **AppName** must be non-empty
4. **LogId** must be valid UUID format
5. **TimestampUtc** must be valid ISO 8601 UTC
6. **Score.Overall/Governance/Safety** must be 0-100
7. **Violations[].Severity** must be one of: Critical, Error, Warning, Info

---

## Error Handling

### Invalid Messages → Dead Letter Queue

Messages that fail validation are sent to the dead letter queue with:
- `DeadLetterReason`: Validation failure type
- `DeadLetterErrorDescription`: Specific error message

### Common Errors

| Error | Reason | Resolution |
|-------|--------|------------|
| `UnsupportedSchema` | Wrong SchemaVersion | Update CerbiStream SDK |
| `InvalidJson` | Malformed JSON | Check serialization |
| `MissingPayload` | Empty body | Verify queue config |
| `validation_failed` | Required fields missing | Check TenantId/AppName |

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-02-03 | Initial specification |
