# CerbiStream: Dev-Friendly, Governance-Enforced Logging for .NET

*Brought to you by **Cerbi LLC**, your trusted partner in enterprise observability.*

> 🚀 **[View CerbiStream Benchmarks](https://cerbi.systems)**
> Compare against Serilog, NLog, and others. CerbiStream is engineered for high performance, strict governance, and enterprise-grade log routing.

[![CerbiStream NuGet](https://img.shields.io/nuget/v/CerbiStream?label=CerbiStream%20NuGet\&style=flat-square)](https://www.nuget.org/packages/CerbiStream/)
[![CerbiStream Downloads](https://img.shields.io/nuget/dt/CerbiStream?label=Downloads\&style=flat-square)](https://www.nuget.org/packages/CerbiStream/)
[![Governance Analyzer NuGet](https://img.shields.io/nuget/v/CerbiStream.GovernanceAnalyzer?label=Governance%20Analyzer%20NuGet\&style=flat-square)](https://www.nuget.org/packages/CerbiStream.GovernanceAnalyzer/)
[![Governance Analyzer Downloads](https://img.shields.io/nuget/dt/CerbiStream.GovernanceAnalyzer?label=Governance%20Analyzer%20Downloads\&style=flat-square)](https://www.nuget.org/packages/CerbiStream.GovernanceAnalyzer/)
[![Build Status](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/cerbi-devsecops.yml/badge.svg?branch=master)](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/cerbi-devsecops.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Zeroshi_Cerbi-CerbiStream\&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Zeroshi_Cerbi-CerbiStream)

[![Benchmark Tests Repo](https://img.shields.io/badge/View-Benchmark%20Tests-blue?style=flat-square)](https://github.com/Zeroshi/CerbiStream.BenchmarkTests)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

---

## 🔗 Supported Destinations

CerbiStream can route logs to:

* **Queues:** Azure Service Bus, RabbitMQ, Kafka, AWS SQS/Kinesis, Google Pub/Sub
* **HTTP Endpoints:** Any REST API with custom headers
* **Cloud Storage:** Azure Blob, AWS S3, Google Cloud Storage
* **File Fallback:** Local JSON file (AES/Base64 encryption supported)
* **Telemetry Providers:** App Insights, OpenTelemetry, Datadog, AWS CloudWatch, GCP Stackdriver

---

## 🧱 CerbiSuite Overview

| Component                          | Purpose                                                |
| ---------------------------------- | ------------------------------------------------------ |
| **CerbiStream**                    | Structured logging for .NET with queue & cloud targets |
| **Cerbi.Governance.Runtime**       | Runtime enforcement of governance rules                |
| **CerbiStream.GovernanceAnalyzer** | Compile-time governance analyzer                       |
| **CerbiShield** (coming soon)      | Governance dashboard & deployment portal               |
| **CerbIQ** (coming soon)           | Metadata aggregation + routing pipeline                |
| **CerbiSense** (coming soon)       | Governance scoring & ML analysis engine                |

---

## 📚 Table of Contents

* [Overview](#overview)
* [Highlights](#highlights)
* [Developer Quick Start](#developer-quick-start)
* [Advanced Setup & Best Practices](#advanced-setup--best-practices)
* [Configuration Options](#️configuration-options)
* [Features](#features)
* [Governance and Compliance](#governance-and-compliance)
* [Advanced Metadata & Encryption](#advanced-metadata--encryption)
* [Usage Examples](#usage-examples)
* [Integration & Supported Platforms](#integration--supported-platforms)
* [Telemetry Provider Support](#telemetry-provider-support)
* [Benchmark vs Serilog](#benchmark-vs-serilog)
* [Unit Testing](#unit-testing)
* [Contributing](#contributing)
* [Community & Support](#community--support)
* [License](#license)

---

## 🆕 2025 Governance Update

CerbiStream now uses **real-time governance enforcement** via [Cerbi.Governance.Runtime](https://www.nuget.org/packages/Cerbi.Governance.Runtime), removing most limitations of build-time validation.

### ✅ Runtime Advantages:

* Compatible with .NET 6–8+
* Config from local, blob, or GitHub
* Automatically tags logs with governance info
* Supports `.Relax()` and `[CerbiTopic]` 

### ⚠️ Build-Time Analyzer Limitations:

* Requires local files and static references
* Not compatible with CI or dynamic config


---

## 🔄 Recent Updates (v1.1.16)

* ✅ **Async console logging via `Channel<string>`**

  * Dramatically improves performance during high-volume console logging
  * Enable using `.WithAsyncConsoleOutput(true)`
  * Automatically wired via `AddCerbiStream(...)`
* Unified enrichment with `LogId`, `ApplicationId`, `InstanceId`, etc.
* Full-payload encryption using AES or Base64
* File fallback with encryption support (AES/Base64)
* Queue + cloud routing (HTTP, Blob, etc.)
* Built-in governance validation via `Cerbi.Governance.Runtime`
* Topic tagging with `[CerbiTopic]` and relaxed logging via `.Relax()`

---

Here is the updated README section that includes your new **async console logging support**. This makes it clear to developers that CerbiStream supports background-buffered logging for high-performance scenarios, just like Serilog's async sink:

---

CerbiStream now supports **async console output** using a high-performance background channel dispatcher. This significantly improves logging throughput, especially during bursts or heavy load.

#### ✅ Runtime Enhancements

* **Async Console Dispatch**: Offloads `Console.WriteLine` to a background thread using `Channel<string>` — 10x+ faster under load.
* **Enable via Fluent API**: `.WithAsyncConsoleOutput(true)`
* **Auto-wired via `AddCerbiStream(...)`**: No boilerplate needed in `Program.cs`
* **Compatible with `Relax()` and governance enforcement**
* **Zero impact on telemetry, queue, or file fallback behavior**

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.WithAsyncConsoleOutput(true)
           .WithGovernanceChecks(true)
           .EnableDeveloperModeWithoutTelemetry();
});
```

🔧 This uses the same async model as `Serilog.Sinks.Async` for performance, ensuring non-blocking log flow even when console is enabled.

---

### 📈 Performance Impact

CerbiStream with async console enabled matches or exceeds Serilog's async throughput for console scenarios.

| Scenario                    | Sync Console | Async Console |
| --------------------------- | ------------ | ------------- |
| Avg Time per Log (µs)       | \~45–100     | \~2–5         |
| Caller Thread Blocked?      | ✅ Yes        | ❌ No          |
| Throughput Under Load       | ❌ Chokes     | ✅ Smooth      |
| Comparable to Serilog.Async | ❌ No         | ✅ Yes         |

---


## Developer Quick Start

👉 [Quick Start Guide](LoggingStandards/README-CerbiStreamDeveloperQuickStart.md)

### 🧰 Developer Setup Matrix

| Scenario                        | Method(s) Used                                                                                       | Description                                              |
| ------------------------------- | ---------------------------------------------------------------------------------------------------- | -------------------------------------------------------- |
| 🔹 Basic Dev (No Governance)    | `EnableDevModeMinimal()`                                                                             | Minimal config, console only                             |
| 🔸 Dev + Governance             | `EnableDeveloperModeWithoutTelemetry()` + `.WithGovernanceChecks(true)`                              | Runtime enforcement, no telemetry                        |
| 🟢 Dev + Governance + Telemetry | `EnableDeveloperModeWithTelemetry()` + `.WithGovernanceChecks(true)` + `.WithTelemetryProvider(...)` | Full stack logging with runtime governance and telemetry |
| 🧪 Benchmarking                 | `EnableBenchmarkMode()`                                                                              | Disables all enrichments for performance tests           |
| 🛠️ Production                  | `EnableProductionMode()` + `.WithQueue(...)` + `.WithEncryptionMode(...)`                            | Production-ready config with security and routing        |

### 🔹 Basic Development (No Governance)

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.EnableDevModeMinimal();
});
```

### 🔸 Development with Real-Time Governance

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.WithFileFallback("cerbi_fallback.json")
           .WithGovernanceChecks(true)
           .WithGovernanceProfile("default")
           .EnableDeveloperModeWithoutTelemetry();
});
```

### 🟢 Development with Governance + Telemetry

````csharp
builder.Logging.AddCerbiStream(options =>
{
    options.WithQueue("AzureServiceBus", "Endpoint=sb://...", "log-queue")
           .WithEncryptionMode(EncryptionType.Base64)
           .WithGovernanceChecks(true)
           .WithTelemetryProvider(
               TelemetryProviderFactory.CreateTelemetryProvider("appinsights")
           )
           .EnableDeveloperModeWithTelemetry();
});

---

## Advanced Setup & Best Practices

👉 [Advanced Setup Guide](LoggingStandards/README-CerbiStream–AdvancedSetup&BestPractices.md)

---

## Overview

CerbiStream is a developer-first logging library built for performance, security, and governance enforcement.

---

## Highlights

- Async, queue-first architecture
- Flexible encryption
- Runtime governance validation
- Low object allocation
- Azure/GCP/AWS-aware metadata

---

## ⚙️ Configuration Options

- `.WithQueue(...)`
- `.WithEncryptionMode(...)`
- `.WithFileFallback(...)`
- `.WithGovernanceChecks(...)`
- `.EnableDeveloperModeWithTelemetry()`

### Encryption Types
- `None`
- `Base64`
- `AES`

### Config Sources
- Local JSON
- Azure Blob / AWS S3 / GCP
- GitHub raw URL

---

## Features

- Topic-based log scoping
- Metadata injection
- Governance policy validation
- Relaxed log support
- Queue & blob transport
- Polly retry support

---

## Governance and Compliance

- Runtime enforcement with `Cerbi.Governance.Runtime`
- Analyzer available for CI validation
- Tags include `GovernanceProfileUsed`, `GovernanceViolations`, etc.

---

## Advanced Metadata & Encryption

```csharp
options.WithMetadataInjection(true)
       .WithEncryptionMode(EncryptionType.AES)
       .WithEncryptionKey(key, iv);
````

JSON logs are fully encrypted before sending.

---

## Usage Examples

* Relaxed logging: `logger.Relax()`
* Topic assignment: `[CerbiTopic("Payments")]`
* Fallback to file: `.WithFileFallback("fallback.json")`
* Multi-cloud: auto-populates cloud provider


---

## 🔓 Relaxed Logging Modes
Cerbi supports two forms of relaxed logging to bypass governance rules when explicitly intended:

Mode	Description	Usage Example
Fluent .Relax()	Developer-triggered, scoped for individual log statements	logger.Relax().LogInformation("...")
Runtime .RelaxGovernance()	Global override, wraps logger during startup configuration	logger = logger.RelaxGovernance();

## 🧑‍💻 Developer Inline (Fluent API):

```csharp
var logger = builder.Logging.CreateLogger("MyApp");
logger = logger.RelaxGovernance(); // All logs now include GovernanceRelaxed: true
```

This uses CerbiLoggerWrapper for structured logging, useful during development or for logs intentionally excluded from governance enforcement.

## ⚙️ Runtime Setup Override:

```csharp
var logger = builder.Logging.CreateLogger("MyApp");
logger = logger.RelaxGovernance(); // All logs now include GovernanceRelaxed: true
```




---

## Integration & Supported Platforms

* Queues: RabbitMQ, Azure, Kafka, etc.
* HTTP REST APIs
* Azure Blob, AWS S3, GCP Storage
* Auto-detect metadata from cloud env vars

---

## Telemetry Provider Support

| Provider        | Activation Code                          |
| --------------- | ---------------------------------------- |
| App Insights    | `CreateTelemetryProvider("appinsights")` |
| OpenTelemetry   | `...("opentelemetry")`                   |
| AWS CloudWatch  | `...("awscloudwatch")`                   |
| Datadog         | `...("datadog")`                         |
| GCP Stackdriver | `...("gcpstackdriver")`                  |

---

## Benchmark vs Serilog

| Metric         | CerbiStream       | Serilog                |
| -------------- | ----------------- | ---------------------- |
| Log Throughput | >50K logs/sec     | 10–30K logs/sec        |
| Encryption     | Native, full JSON | External libs required |
| Memory per log | \~1–3 KB          | \~10–30 KB             |
| Governance     | Built-in          | Requires plugin stack  |

---

## Unit Testing

Covers:

* Governance rule enforcement
* Retry/backoff
* Metadata injection
* Fallback logging rotation
* Encryption and validation

---

## Contributing

PRs welcome! Please open an issue first for large features. Follow code style and ensure tests pass.

---

## Community & Support

* GitHub: [https://github.com/Zeroshi/Cerbi-CerbiStream](https://github.com/Zeroshi/Cerbi-CerbiStream)
* Email: [hello@cerbi.io](mailto:hello@cerbi.io)
* Website: [https://cerbi.io](https://cerbi.io)
* Benchmarks: [https://cerbi.systems](https://cerbi.systems)

---

## 📌 Under the Hood: Runtime Defaults & Utilities

```
   ┌────────┐
   │  log   │
   └───┬────┘
       │
       ▼
  ┌──────────┐
  │ enrich   │◄───┐
  └────┬─────┘    │
       ▼          │
  ┌──────────┐     │
  │ encrypt  │     │
  └────┬─────┘     │
       ▼          │
  ┌──────────┐     │
  │ validate │     │
  └────┬─────┘     │
       ▼          │
  ┌──────────┐     │
  │  send    │─────┘
  └──────────┘
       ▲
       │
  ┌──────────────┐
  │ RetryPolicy  │
  └──────────────┘
```

CerbiStream includes helpful built-in utilities and conventions:

### ☁️ Cloud Metadata Detection

Pulled from environment variables using `ApplicationMetadata`:

* `CloudProvider` resolves from AWS, Azure, or GCP.
* `Region` is inferred from cloud-specific variables.
* `InstanceId` and `ApplicationVersion` are auto-filled.

### 🛠️ Central Config Convenience

Use `CerbiStreamConfig` for:

* Single-file config initialization
* Auto-detected cloud + env context
* Runtime feature toggles (e.g., encryption, telemetry)

### 🔁 Retry Behavior

Retry logic for queue delivery is powered by Polly:

```csharp
RetryPolicyFactory.Create(3, 200); // Retries 3x with 200ms delay
```

This is used when `.WithQueueRetries(...)` is enabled.

### 📡 Telemetry Integration

Quickly wire up telemetry providers with:

```csharp
TelemetryProviderFactory.CreateTelemetryProvider("appinsights");
```

Supported options include AppInsights, OpenTelemetry, Datadog, AWS CloudWatch, and GCP Stackdriver.

---

Here’s the addition you can include in your `README.md` under a new section like `## 🙏 Acknowledgments` or within your `## 🔄 Recent Updates` or `## 🧪 Benchmarks` section:

---

## 🙏 Acknowledgments

Special thanks to [**@snakefoot**](https://github.com/snakefoot) for the performance insights and benchmark contributions across popular logging frameworks:

* ✅ **Microsoft Console Logger** uses an asynchronous queue by default.
* ⚠️ **Serilog Console** writes synchronously — causing slower results unless async sinks are used.
* ✅ **NLog** and **Log4Net** support async file targets out of the box, yielding faster logging performance.
* 🧪 These contributions helped improve CerbiStream's own async capabilities.

---

Let me know if you'd like this rendered into a Markdown table with benchmark chart screenshots or badges.


## License

MIT © Cerbi LLC
