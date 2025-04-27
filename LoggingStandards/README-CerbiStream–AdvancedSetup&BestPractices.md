---
# CerbiStream – Advanced Setup & Best Practices

> This guide covers how to configure optional advanced features for **CerbiStream** to maximize reliability, observability, and compliance.

---

## 1. Retry Policies (Queue Failures)

CerbiStream supports automatic retry logic if sending logs to a queue fails.

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.WithQueueRetries(enabled: true, retryCount: 5, delayMilliseconds: 500);
});
```

| Option | Purpose |
|:---|:---|
| `retryCount` | Max number of retry attempts |
| `delayMilliseconds` | Delay between retries |

✅ **Recommended:** Enable retries for production workloads.

---

## 2. Telemetry Context Enrichment

Set these fields to enrich App Insights / OpenTelemetry traces automatically:

```csharp
TelemetryContext.ServiceName = "OrderService";
TelemetryContext.OriginApp = "FrontendApp";
TelemetryContext.Feature = "CheckoutFlow";
TelemetryContext.UserType = "Customer";
TelemetryContext.IsRetry = false;
TelemetryContext.RetryAttempt = 0;
```

These values will be injected into:
- All structured logs
- Telemetry events (if enabled)

✅ **Recommended:** Set per-request for maximum context.

---

## 3. Governance Validation (Optional)

You can provide a custom validation function to reject non-compliant logs:

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.WithGovernanceValidator((profileName, logData) =>
    {
        return logData.ContainsKey("TraceId") && logData.ContainsKey("ServiceName");
    });
});
```

| Behavior | |
|:---|:---|
| ✅ Return `true` | Log passes validation |
| ❌ Return `false` | Log is **dropped** and a warning is emitted |

✅ **Recommended:** Use for strict regulatory environments.

---

## 4. Lightweight Tracing Support

Enable automatic injection of TraceId, SpanId, ParentSpanId metadata:

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.WithTracingEnrichment(true);
});
```

Or use **Minimal Mode** to skip tracing for max performance:

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.EnableMinimalMode();
});
```

✅ **Recommended:** Always enable tracing for distributed systems.

---

## 5. Fallback File Logging (Highly Recommended)

Capture logs locally if the queue is unavailable.

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.FileFallback = new FileFallbackOptions
    {
        DirectoryPath = "/var/logs/cerbi",
        FileNamePrefix = "cerbi",
        MaxFileSizeMB = 100
    };
});
```

✅ **Recommended:** Always enable fallback in production.

---

# ✅ Conclusion

CerbiStream can operate:
- Minimal overhead (fastest mode)
- Full compliance (governed mode)
- Full observability (telemetry + tracing)

The power is yours to choose. 🔥

---

[Back to QuickStart →](./CERBISTREAM_QUICKSTART.md)
