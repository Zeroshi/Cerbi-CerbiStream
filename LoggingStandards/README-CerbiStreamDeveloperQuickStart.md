# CerbiStream Developer QuickStart

CerbiStream makes structured logging easy — and powerful — across cloud-native apps.

---

## 🛠️ Basic Logger Setup

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.WithApplicationIdentity("WebApp", "OrderService")
           .WithTargetSystem("PaymentAPI", "PaymentService")
           .WithTelemetryProvider(TelemetryProviderFactory.CreateTelemetryProvider("appinsights"))
           .EnableFullMode() // Capture TraceId, SpanId, ParentSpanId automatically
           .WithTelemetryLogging(true)
           .WithConsoleOutput(true);
});
```

---

## ✨ Dynamic Metadata (Per Request)

| Field | Example | When to Set |
|:------|:--------|:------------|
| `TelemetryContext.Feature` | `"CheckoutFlow"` | At start of logical flow |
| `TelemetryContext.IsRetry` | `true` or `false` | Inside retry logic |
| `TelemetryContext.RetryAttempt` | `1`, `2`, etc. | When retrying a failed call |

Example:

```csharp
TelemetryContext.Feature = "CheckoutFlow";
TelemetryContext.IsRetry = false;
TelemetryContext.RetryAttempt = 0;

_logger.LogInformation("User began checkout flow");
```

---

## 🧹 Cleaning Up After the Request

```csharp
TelemetryContext.Clear();
```

Call this at the end of a controller or inside middleware to prevent leaking telemetry info across requests.

---

## 🧐 How TraceId, SpanId, ParentSpanId Work

- Enabled automatically if `EnableFullMode()` is set.
- No manual configuration needed.
- Supports distributed tracing (Azure App Insights, AWS X-Ray, OpenTelemetry, etc.).

---

## 📊 Example Full Log Output (Metadata)

```json
{
  "TimestampUtc": "2025-04-27T13:45:00Z",
  "ServiceName": "OrderService",
  "OriginApp": "MyFrontendApp",
  "ApplicationType": "WebApp",
  "ServiceType": "OrderProcessing",
  "Feature": "CheckoutFlow",
  "IsRetry": false,
  "RetryAttempt": 0,
  "TraceId": "f8c3de09c7b2477898321839a1234567",
  "SpanId": "a1b2c3d4e5f6g7h8",
  "ParentSpanId": "1122334455667788"
}
```

---

# 🔥 CerbiStream Design Principles

- **Developer First**: Easy to add context without friction.
- **Flexible**: Structured metadata without rigid schemas.
- **Observability Ready**: Compatible with OpenTelemetry, App Insights, Datadog, CloudWatch, Stackdriver.
- **Governance Built-In**: Structured log validation ready (HIPAA/GDPR compliance support).

---

# ✅ Quick Checklist

| | |
|-|-|
| Set Service Identity | In `AddCerbiStream(options => ...)` |
| Set Dynamic Fields (Feature, Retry) | At runtime during request |
| Clear Context | `TelemetryContext.Clear()` after request |
| Tracing | Enabled with `EnableFullMode()` |

---

# 🛡️ Enterprise-Grade Logging, Developer Simplicity.

CerbiStream — built for performance, observability, and scale.
