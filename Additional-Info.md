# CerbiStream: Developer-Friendly, Governance-Enforced Logging for .NET

CerbiStream is a structured logging library tailored for .NET applications, emphasizing performance, security, and governance. It facilitates consistent log structures, supports multiple output destinations, and enforces logging policies at runtime.

## CerbiSuite Components

CerbiStream is part of the broader CerbiSuite, which includes:

* **CerbiStream**: Structured logging for .NET with support for queues and cloud targets.
* **Cerbi.Governance.Runtime**: Runtime enforcement of governance rules.
* **CerbiStream.GovernanceAnalyzer**: Compile-time governance analyzer.
* **CerbiShield** (coming soon): Governance dashboard and deployment portal.
* **CerbIQ** (coming soon): Metadata aggregation and routing pipeline.
* **CerbiSense** (coming soon): Governance scoring and ML analysis engine.

## Getting Started

### Installation

```bash
dotnet add package CerbiStream
```

### Basic Configuration

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.EnableDevModeMinimal();
});
```

This sets up CerbiStream with minimal configuration, suitable for development environments.

## Configuration Options

| Method                                          | Description                                                      | Example                                                                   | When to Use                                                 |
| ----------------------------------------------- | ---------------------------------------------------------------- | ------------------------------------------------------------------------- | ----------------------------------------------------------- |
| `WithTenantId(tenantId)`                        | Sets tenant identifier for multi-tenant scoring.                 | `.WithTenantId("my-tenant-123")`                                          | Required for Scoring API. Falls back to governance config.  |
| `WithQueue(type, host, name)`                   | Configures which message queue to send logs to.                  | `.WithQueue("RabbitMQ", "localhost", "logs-queue")`                       | Use for structured delivery of logs to centralized systems. |
| `WithDisableQueue(true)`                        | Skips queue delivery entirely.                                   | `.WithDisableQueue(true)`                                                 | For debugging, file-only modes, or HTTP-only scenarios.     |
| `WithTelemetryProvider(provider)`               | Sends logs to a telemetry backend like App Insights or Datadog.  | `.WithTelemetryProvider(CreateTelemetryProvider("appinsights"))`          | Enables centralized metrics and tracing.                    |
| `WithQueueRetries(bool, retryCount, delayMs)`   | Adds retry logic for queue delivery using Polly.                 | `.WithQueueRetries(true, 3, 200)`                                         | Increases resilience on transient failures.                 |
| `WithFileFallback(...)`                         | Logs to JSON file if queue fails.                                | `.WithFileFallback("logs/fallback.json")`                                 | Ensures logs are not lost on transport failures.            |
| `WithEncryptedFallback(...)`                    | Adds encryption and rotation settings to fallback files.         | `.WithEncryptedFallback("fallback.json", "primary.json", key, iv)`        | Protects logs at rest when fallback is used.                |
| `WithEncryptionMode(mode)`                      | Encrypts payload before transport.                               | `.WithEncryptionMode(EncryptionType.AES)`                                 | Use for sensitive logs (PII/PHI/etc.).                      |
| `WithEncryptionKey(key, iv)`                    | Supplies encryption keys when AES is used.                       | `.WithEncryptionKey(keyBytes, ivBytes)`                                   | Required for AES encryption.                                |
| `WithGovernanceChecks(true)`                    | Enables runtime validation of log structure.                     | `.WithGovernanceChecks(true)`                                             | Core Cerbi governance enforcement.                          |
| `WithGovernanceValidator(...)`                  | Custom validation delegate (overrides runtime validator).        | `.WithGovernanceValidator((profile, meta) => meta.ContainsKey("userId"))` | For advanced rule bypasses or overrides.                    |
| `WithMetadataInjection(true)`                   | Auto-injects timestamp, log level, etc.                          | `.WithMetadataInjection(true)`                                            | Recommended for all environments.                           |
| `WithAdvancedMetadata(true)`                    | Adds cloud info, region, app ID, etc.                            | `.WithAdvancedMetadata(true)`                                             | Enables full observability.                                 |
| `WithSecurityMetadata(true)`                    | Injects redacted or masked fields.                               | `.WithSecurityMetadata(true)`                                             | Useful for compliance/security review.                      |
| `WithTracingEnrichment(true)`                   | Adds `TraceId`, `SpanId`, `ParentSpanId` from current activity.  | `.WithTracingEnrichment(true)`                                            | Enables lightweight distributed tracing.                    |
| `WithApplicationIdentity(appType, serviceType)` | Adds custom service descriptors.                                 | `.WithApplicationIdentity("WebApp", "PaymentService")`                    | Helps classify logs by business context.                    |
| `WithTargetSystem(appType, serviceType)`        | Identifies dependencies or downstream targets.                   | `.WithTargetSystem("Worker", "OrderQueue")`                               | Improves cross-service correlation.                         |
| `WithTelemetryLogging(true)`                    | Enables sending logs to telemetry provider in addition to queue. | `.WithTelemetryLogging(true)`                                             | Enables dual-destination delivery.                          |
| `WithConsoleOutput(true)`                       | Enables writing logs to console.                                 | `.WithConsoleOutput(true)`                                                | Helpful during development and local testing.               |

CerbiStream offers a variety of configuration methods to tailor logging behavior:

### Queue Configuration

```csharp
options.WithQueue("AzureServiceBus", "Endpoint=sb://...", "log-queue");
```

Routes logs to Azure Service Bus, RabbitMQ, Kafka, AWS SQS, Google Pub/Sub.

### Telemetry Integration

```csharp
options.WithTelemetryProvider(
    TelemetryProviderFactory.CreateTelemetryProvider("appinsights")
);
```

Supports App Insights, OpenTelemetry, Datadog, AWS CloudWatch, GCP Stackdriver.

### Encryption

```csharp
options.WithEncryptionMode(EncryptionType.AES)
       .WithEncryptionKey(key, iv);
```

Supports None, Base64, and AES for securing log payloads.

### File Fallback

```csharp
options.WithFileFallback("logs/fallback.json");
```

Writes logs to local JSON if delivery fails.

### Governance Enforcement

```csharp
options.WithGovernanceChecks(true);
```

Enables governance profile enforcement at runtime.

### Metadata Injection

```csharp
options.WithMetadataInjection(true)
       .WithAdvancedMetadata(true)
       .WithSecurityMetadata(true);
```

Injects timestamps, log level, cloud info, and secure fields.

## Governance and Compliance

* **Runtime Enforcement**: Uses Cerbi.Governance.Runtime to validate logs against defined profiles.
* **Relaxed Logging**: Bypasses governance via `logger.Relax()`.
* **Topic Tagging**: Assigns topics via `[CerbiTopic("Orders")]` attribute.

## Usage Examples

### Relaxed Logging

```csharp
logger.Relax().LogInformation("This log bypasses governance checks.");
```

```json
{
  "Message": "This log bypasses governance checks.",
  "GovernanceRelaxed": true,
  "TimestampUtc": "2025-05-19T03:21:15Z",
  "LogLevel": "Information",
  "CloudProvider": "Azure",
  "Region": "eastus",
  "InstanceId": "machine-xyz",
  "ApplicationVersion": "1.2.3"
}
```

### Topic Assignment

```csharp
[CerbiTopic("Payments")]
public class PaymentProcessor { }
```

### Valid Structured Log

```csharp
logger.LogInformation("New Order", new Dictionary<string, object>
{
    ["userId"] = "u123",
    ["email"] = "user@example.com"
});
```

```json
{
  "Message": "New Order",
  "userId": "u123",
  "email": "user@example.com",
  "GovernanceProfileUsed": "Orders",
  "TimestampUtc": "2025-05-19T03:22:45Z",
  "LogLevel": "Information"
}
```

### Violation Example (Missing Required Fields)

```csharp
logger.LogWarning("Login Failed", new Dictionary<string, object>
{
    ["note"] = "userId missing"
});
```

```json
{
  "Message": "Login Failed",
  "note": "userId missing",
  "GovernanceViolations": ["Missing: userId"],
  "GovernanceMode": "Strict",
  "TimestampUtc": "2025-05-19T03:23:11Z",
  "LogLevel": "Warning"
}
```

### Fallback Logging

```csharp
options.WithFileFallback("logs/fallback.json");
```

Logs will be written to disk in encrypted format when queue delivery fails:

```json
[ENCRYPTED]<base64-or-AES-payload>[/ENCRYPTED]
```

## 🚀 Preset Modes

You can also use preconfigured modes to simplify setup:

```csharp
options.EnableDevModeMinimal();             // Console only, no metadata, no governance
options.EnableDeveloperModeWithoutTelemetry(); // Metadata injected, no telemetry
options.EnableDeveloperModeWithTelemetry();    // Metadata + telemetry, governance disabled
options.EnableProductionMode();               // Full governance, encryption, telemetry
options.EnableBenchmarkMode();                // No outputs, for performance testing
```

## Benchmarking

* **Performance**: Faster than Microsoft.Extensions.Logging and comparable to Serilog.
* **Memory Usage**: Minimal footprint, even with encryption.
* **Benchmarks**: See [CerbiStream Benchmark Tests](https://github.com/Zeroshi/CerbiStream.BenchmarkTests)

## Contributing

Contributions welcome — see `CONTRIBUTING.md`.

## License

MIT License — see `LICENSE` file.

---

For more, visit the [CerbiStream GitHub Repository](https://github.com/Zeroshi/Cerbi-CerbiStream)
