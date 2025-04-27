# CerbiStream: Dev-Friendly, Governance-Enforced Logging for .NET

*Brought to you by **Cerbi LLC**, your trusted partner in enterprise observability.*

> 🚀 **[View CerbiStream Benchmarks](https://cerbi.systems)**  
> Compare against Serilog, NLog, and others. CerbiStream is engineered for high performance, strict governance, and enterprise-grade log routing.

[![CerbiStream NuGet](https://img.shields.io/nuget/v/CerbiStream?label=CerbiStream%20NuGet&style=flat-square)](https://www.nuget.org/packages/CerbiStream/)
[![CerbiStream Downloads](https://img.shields.io/nuget/dt/CerbiStream?label=Downloads&style=flat-square)](https://www.nuget.org/packages/CerbiStream/)
[![Governance Analyzer NuGet](https://img.shields.io/nuget/v/CerbiStream.GovernanceAnalyzer?label=Governance%20Analyzer%20NuGet&style=flat-square)](https://www.nuget.org/packages/CerbiStream.GovernanceAnalyzer/)
[![Governance Analyzer Downloads](https://img.shields.io/nuget/dt/CerbiStream.GovernanceAnalyzer?label=Governance%20Analyzer%20Downloads&style=flat-square)](https://www.nuget.org/packages/CerbiStream.GovernanceAnalyzer/)
[![Build Status](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/cerbi-devsecops.yml/badge.svg?branch=master)](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/cerbi-devsecops.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Zeroshi_Cerbi-CerbiStream&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Zeroshi_Cerbi-CerbiStream)
[![Benchmark Tests Repo](https://img.shields.io/badge/View-Benchmark%20Tests-blue?style=flat-square)](https://github.com/Zeroshi/CerbiStream.BenchmarkTests)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)


---

## Table of Contents

- [Overview](#overview)
- [Highlights](#highlights)
- [Developer Quick Start](#developer-quick-start)
- [Advanced Setup & Best Practices](#advanced-setup--best-practices)
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

## Developer Quick Start

👉 [View Quick Start Guide](LoggingStandards/README-CerbiStreamDeveloperQuickStart.md)

## Advanced Setup & Best Practices

👉 [View Advanced Setup Guide](LoggingStandards/README-CerbiStream–AdvancedSetup&BestPractices.md)

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

# CerbiStream: Dev-Friendly, Governance-Enforced Logging for .NET

*Brought to you by **Cerbi LLC**, your trusted partner in enterprise observability.*

> 🚀 **[View CerbiStream Benchmarks](https://cerbi.systems)**  
> Compare against Serilog, NLog, and others. CerbiStream is engineered for high performance, strict governance, and enterprise-grade log routing.

---

## Benchmark vs Serilog

**CerbiStream** was benchmarked against **Serilog** to highlight performance, memory usage, and enterprise-readiness.

| Category | CerbiStream | Serilog | Comments |
|:---------|:------------|:--------|:---------|
| **Pipeline Layers** | Serialize → Encrypt → Send | Destructure ➔ Enrich ➔ Filter ➔ Format ➔ Write | Serilog's architecture adds overhead per log event. |
| **Memory Allocation per Event** | 🔵 ~1–3 KB | 🔴 ~10–30 KB | Serilog allocates multiple objects (LogEventProperty trees). |
| **Encryption Model** | Encrypt entire payload | No built-in encryption | CerbiStream secures the full log payload without external libraries. |
| **Throughput** | 🟢 >50K logs/sec | 🟠 ~10K–30K logs/sec | CerbiStream maintains speed even with encryption and governance. |
| **Telemetry & Governance** | Native, lightweight, optional enforcement | Plugins required, no native governance | CerbiStream enforces compliance simply. |

---

### Why CerbiStream is Faster

- **No Multi-Step Enrichers**: Metadata is injected once during serialization.
- **Full-Payload Encryption**: Encrypts the JSON payload, avoiding per-field cost.
- **Minimal Object Allocation**: No construction of complex `LogEvent` trees.
- **Light Retry Strategy**: Built-in Polly retries for resilient delivery.

---

### When to Choose CerbiStream

| | Enterprise-Grade Systems | Lightweight Applications |
|:-|:-|:-|
| **CerbiStream** | ✅ Best choice (performance, compliance, encryption) | ✅ Simple setup, lower resource usage |
| **Serilog** | ⚙️ Possible, but heavier and harder to govern | ✅ Best for flexible, custom plug-in scenarios |

**View the full benchmarks:** [CerbiStream Benchmark Tests](https://github.com/Zeroshi/CerbiStream.BenchmarkTests)


---

## License

MIT © Cerbi LLC



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

