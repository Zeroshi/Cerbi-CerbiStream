# CerbiStream: Dev-Friendly, Governance-Enforced Logging for .NET

*Brought to you by **Cerbi LLC**, your trusted partner in enterprise observability.*

> 🚀 **[View CerbiStream Benchmarks](https://cerbi.systems)**  
> Compare against Serilog, NLog, and others. CerbiStream is engineered for high performance, strict governance, and enterprise-grade log routing.

---

## Table of Contents

- [Overview](#overview)
- [Highlights](#highlights)
- [Features](#features)
- [Architecture & Implementation](#architecture--implementation)
- [Preset Modes and Configuration](#preset-modes-and-configuration)
- [Advanced Metadata & Encryption](#advanced-metadata--encryption)
- [Usage Examples](#usage-examples)
- [Integration & Supported Platforms](#integration--supported-platforms)
- [Governance and Compliance](#governance-and-compliance)
- [Telemetry Provider Support](#telemetry-provider-support)
- [Unit Testing](#unit-testing)
- [Contributing](#contributing)
- [Community & Support](#community--support)
- [License](#license)

---

## 🔄 Recent Updates (v1.1.2)

### New Features

- **Unified Log Enrichment:** Every log is decorated with a `LogId`, `TimestampUtc`, `ApplicationId`, `InstanceId`, `CloudProvider`, `Region`, plus any user metadata.
- **Payload Encryption:** When `EncryptionMode` is set (Base64/AES) **and** encryption is enabled, the full JSON payload is encrypted *before* sending. A debug entry logs:
  ```csharp
  _logger.LogDebug($"[CerbiStream] Payload for log ID {logId} encrypted ({options.EncryptionMode}).");
  ```
- **Metadata Injection:** With `EnableMetadataInjection`, every call automatically adds timestamp, log level, and (optionally) encrypts sensitive metadata fields (`APIKey`, `SensitiveField`, etc.).
- **Governance Hook:** Before sending, `options.ValidateLog(profileName, metadata)` runs any configured governance validator; failures drop the log with an error.
- **New Backends Added:** Out-of-the-box support now includes **HTTP endpoint** (`HttpMessageSender`) and **Azure Blob Storage** (`BlobStorageSender`)—a highlight of v1.1.2!

---

## Overview

**CerbiStream** is a high-performance, dev-friendly logging framework for .NET that enforces structured logging governance and flexible encryption. It integrates seamlessly with `ILogger<T>` and supports a variety of backends.

---

## Highlights

- **High Throughput:** Async, queue‑first architecture minimizes latency.
- **Governance Enforced:** Schema and field validation via pluggable validators.
- **Encryption Options:** Base64 or AES encrypt entire JSON payloads.
- **Telemetry Integration:** Forward events/exceptions/dependencies to App Insights, Datadog, etc.
- **Fallback Logging:** Optional encrypted file rotation when queues or endpoints are unavailable.

---

## Features

- **Preset Modes:**
  - `EnableDevModeMinimal()` — Console only, no metadata/governance.
  - `EnableDeveloperModeWithoutTelemetry()` — Metadata injected, no telemetry.
  - `EnableDeveloperModeWithTelemetry()` — Full metadata + telemetry.
  - `EnableBenchmarkMode()` — Silent/benchmark mode.

- **Advanced Metadata Injection:**
  - Automatic `TimestampUtc`, `LogLevel`, plus any custom fields.
  - Conditional encryption of sensitive fields.

- **Payload Encryption:**
  - `EncryptionType.Base64` or `AES` with configurable key/IV.
  - Debug logs show encryption actions.

- **Transport Agnostic:**
  - **Queues:** RabbitMQ, Kafka, Azure Service Bus, AWS SQS/Kinesis, Google Pub/Sub.
  - **HTTP:** Post JSON logs to any REST endpoint.
  - **Azure Blob:** Store logs as blobs for later processing.

- **Polly Retries:**
  - Transparent retry logic for transient failures.

- **Governance Validator:**
  - Drop or enrich messages based on organizational policies.

---

## Advanced Metadata & Encryption

### Metadata Injection

Enable or disable via:
```csharp
options.WithMetadataInjection(true);
```
When enabled, logs include:
- `TimestampUtc` (UTC time)
- `LogLevel` (string)
- Any custom metadata you supply.

### Payload Encryption

Configure in `CerbiStreamOptions`:
```csharp
options.WithEncryptionMode(EncryptionType.AES)
       .WithEncryptionKey(myKeyBytes, myIvBytes);
```
When enabled, the entire JSON payload is encrypted *before* transport, and a debug log indicates this.

#### Before Encryption
```json
{
  "LogId": "abc123",
  "LogData": { /* your log entry */ }
}
```
#### After Encryption
```
[ENCRYPTED]<base64-or-aes-payload>[/ENCRYPTED]
```

---

## Usage Examples

### By Environment

#### Development
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddCerbiStream(opts => opts.EnableDevModeMinimal());
var app = builder.Build();
app.Run();
```

#### Production
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddCerbiStream(opts => {
    opts.WithQueue("RabbitMQ", "amqp://localhost", "logs-queue")
        .WithEncryptionMode(EncryptionType.Base64)
        .WithMetadataInjection(true)
        .EnableDeveloperModeWithoutTelemetry();
});
```  

### By Transport

#### Queues
```csharp
builder.Logging.AddCerbiStream(opts => {
    opts.WithQueue("AzureServiceBus", connectionString, "my-logs")
        .WithEncryptionMode(EncryptionType.AES)
        .WithEncryptionKey(key, iv)
        .WithTelemetryProvider(
            TelemetryProviderFactory.CreateTelemetryProvider("appinsights")
        )
        .WithGovernanceValidator((profile, md) => md.ContainsKey("UserId"))
        .EnableDeveloperModeWithTelemetry();
});
```

#### HTTP
```csharp
builder.Services.AddSingleton<ISendMessage>(
    new HttpMessageSender("https://logs.myservice.com/ingest", new Dictionary<string,string>{{"x-api-key","secret"}})
);
```

#### Blob Storage
```csharp
builder.Services.AddSingleton<ISendMessage>(
    new BlobStorageSender(connectionString, "logs-container")
);
```

#### Merged Performance & Exception
```csharp
var metadata = new Dictionary<string, object> {
    ["ElapsedMilliseconds"] = 200,
    ["DependencyCommand"]  = "POST /api/order",
    ["ExceptionMessage"]   = ex.Message
};
await logger.LogEventAsync("OrderError", LogLevel.Error, metadata);
```

---

## Integration & Supported Platforms

**Queues:** RabbitMQ, Kafka, Azure Service Bus, AWS SQS/Kinesis, Google Pub/Sub.  
**HTTP Endpoint:** Any RESTful API.  
**Azure Blob Storage:** Append logs as JSON blobs.

**Auto‑Detect Cloud Metadata:** CerbiStream inspects these environment variables:
- `AWS_EXECUTION_ENV` → AWS  
- `GOOGLE_CLOUD_PROJECT` → GCP  
- `WEBSITE_SITE_NAME`      → Azure

These populate `CloudProvider` and `Region` metadata fields.

---

## Governance and Compliance

- **Pluggable Validator:** `options.WithGovernanceValidator(...)` to enforce fields.
- **Static Analysis:** Use **CerbiStream.GovernanceAnalyzer** in your CI:
  ```yaml
  # .github/workflows/governance.yml
  name: Governance Validation
  on: [push, pull_request]
  jobs:
    validate-logs:
      runs-on: ubuntu-latest
      steps:
        - uses: actions/checkout@v2
        - name: Install Analyzer
          run: dotnet tool install -g CerbiStream.GovernanceAnalyzer
        - name: Run Analyzer
          run: cerbi-governance analyze --config cerbi_governance.json
  ```
- **Azure Marketplace Dashboard:** Coming soon – visualize governance metrics in CerbiStream dashboard.

---

## Telemetry Provider Support

| Provider           | Activation                                    |
|--------------------|-----------------------------------------------|
| OpenTelemetry      | `CreateTelemetryProvider("opentelemetry")`   |
| App Insights       | `CreateTelemetryProvider("appinsights")`     |
| AWS CloudWatch     | `CreateTelemetryProvider("awscloudwatch")`   |
| Datadog            | `CreateTelemetryProvider("datadog")`         |
| GCP Stackdriver    | `CreateTelemetryProvider("gcpstackdriver")`  |

---

## Unit Testing

Comprehensive coverage for:
- Payload encryption
- Metadata injection
- Retry logic
- Governance validation
- File fallback rotation

---

## Contributing

PRs and issues welcome! Please ensure tests pass and follow style guidelines. Use `git tag` to list releases.

---

## Community & Support

- **GitHub Repo:** https://github.com/Zeroshi/Cerbi-CerbiStream  
- **Email:** hello@cerbi.io  
- **Website & Benchmarks:** https://cerbi.systems  
- **Governance Analyzer:** https://www.nuget.org/packages/CerbiStream.GovernanceAnalyzer

---

## License

MIT © Cerbi LLC

