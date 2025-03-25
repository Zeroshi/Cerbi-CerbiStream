# CerbiStream Logging Library

![NuGet](https://img.shields.io/nuget/v/CerbiStream?style=flat-square)
![NuGet Downloads](https://img.shields.io/nuget/dt/CerbiStream?style=flat-square)
![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=flat-square)
![Dev Friendly](https://img.shields.io/badge/dev--friendly-%E2%9C%94%EF%B8%8F-brightgreen?style=flat-square)
![Governance Enforced](https://img.shields.io/badge/governance-enforced-red?style=flat-square)
![CI/CD](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/dotnet.yml/badge.svg?style=flat-square)

CerbiStream is a **next-generation** logging solution built for **structured logs, governance enforcement, and multi-destination routing**. It ensures secure, consistent, and high-performance logging for **cloud, on-prem, and hybrid environments**.

---

## 🚀 What's New?

CerbiStream v1.0.9 introduces a modernized, simplified configuration model designed for speed, clarity, and real-world use cases.

### 🔧 Major Improvements
- **New Preset Configuration Modes** – Easily switch between `DeveloperModeWithTelemetry`, `MinimalMode`, or `BenchmarkMode` without manually toggling settings.
- **Cleaner Developer Experience** – Legacy `EnableDevMode()` removed in favor of intuitive preset APIs.
- **Fine-Grained Controls** – New methods like `DisableConsoleOutput()`, `DisableGovernanceChecks()`, and `DisableMetadataInjection()` provide precise control.
- **Better Benchmarking** – `BenchmarkMode()` disables everything unnecessary for micro-benchmark testing.
- **Telemetry Separation** – Decouple telemetry tracking from core logging logic with `EnableTelemetryLogging()`.

These improvements reflect feedback from real production and OSS usage—balancing governance enforcement with performance and flexibility.

---

## 🧰 Getting Started

CerbiStream works out-of-the-box. Install, configure, and start logging:

1. Install the NuGet package
2. Set your queue and enrichment metadata
3. Start logging with `ILogger<T>`

→ For governance enforcement, install the [GovernanceAnalyzer](https://www.nuget.org/packages/CerbiStream.GovernanceAnalyzer).

---

## 📦 Installation

Install CerbiStream from NuGet:

```sh
dotnet add package CerbiStream
```

To enable governance validation:

```sh
dotnet add package CerbiStream.GovernanceAnalyzer
```

---

## ⚙️ Preset Config Modes

CerbiStream provides ready-to-use configuration presets:

| Method                                | Description                                                              |
|--------------------------------------|--------------------------------------------------------------------------|
| `EnableDeveloperModeWithTelemetry()` | Console + metadata + telemetry (for dev/test)                            |
| `EnableDeveloperModeWithoutTelemetry()` | Console + metadata, no telemetry (clean dev logs)                       |
| `EnableDevModeMinimal()`             | Console only (no metadata or telemetry) for benchmarks or POCs           |
| `EnableBenchmarkMode()`              | All silent — disables output, telemetry, metadata, and governance        |

---

## 🔧 Individual Configuration Options

For full control over behavior, you can toggle each capability manually:

| Option                      | Description                                                              |
|-----------------------------|--------------------------------------------------------------------------|
| `DisableConsoleOutput()`    | Prevents log messages from appearing in local console                    |
| `DisableTelemetryEnrichment()` | Disables automatic telemetry context injection (e.g., ServiceName, etc.) |
| `DisableMetadataInjection()` | Skips adding common metadata fields (e.g., user type, retry count)       |
| `DisableGovernanceChecks()` | Bypasses governance schema validation on log structure                   |
| `IncludeAdvancedMetadata()` | Adds environment/cloud-specific fields like region, version, etc.        |
| `IncludeSecurityMetadata()` | Adds security context fields, if applicable                              |
| `SetTelemetryProvider()`    | Manually inject a telemetry routing provider                             |
| `EnableTelemetryLogging()`  | Sends logs to telemetry independently of main log queue                  |

Use these when building custom setups or combining multiple concerns.

---

## ⚡ Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CerbiStream;

var serviceProvider = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConsole();
        builder.AddCerbiStream(options =>
        {
            options.SetQueue("RabbitMQ", "localhost", "logs-queue");
            options.EnableDeveloperModeWithoutTelemetry();

            TelemetryContext.ServiceName = "CheckoutService";
            TelemetryContext.OriginApp = "MyFrontendApp";
            TelemetryContext.UserType = "InternalUser";
        });
    })
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("App started");
```

---

## 🔧 Dev Helper Shortcut

```csharp
builder.AddDevLogging(); // applies EnableDeveloperModeWithoutTelemetry + telemetry context
```

---

## 🌐 Supported Logging Destinations

| Queue Type            | Example Value         |
|----------------------|-----------------------|
| RabbitMQ             | "RabbitMQ"            |
| Kafka                | "Kafka"               |
| Azure Queue Storage  | "AzureQueue"          |
| Azure Service Bus    | "AzureServiceBus"     |
| AWS SQS              | "AWS_SQS"             |
| AWS Kinesis          | "AWS_Kinesis"         |
| Google Pub/Sub       | "GooglePubSub"        |

---

## 🔍 Automatic Metadata Fields

| Field                | Auto-Detected? | Example       |
|----------------------|----------------|---------------|
| CloudProvider        | ✅             | Azure         |
| Region               | ✅             | us-east-1     |
| Environment          | ✅             | Production    |
| ApplicationVersion   | ✅             | v1.2.3        |
| RequestId            | ✅             | abc123        |
| TransactionType      | ❌             | REST          |
| TransactionStatus    | ❌             | Success       |

---

## ✅ Telemetry Context Fields

- `ServiceName`
- `OriginApp`
- `UserType`
- `Feature`
- `IsRetry`
- `RetryAttempt`

Set them once and they’ll enrich all logs.

---

## 🔁 Retry Tracking Example

```csharp
Policy
  .Handle<Exception>()
  .WaitAndRetry(3, _ => TimeSpan.FromSeconds(1), (ex, _, attempt, _) =>
  {
      TelemetryContext.IsRetry = true;
      TelemetryContext.RetryAttempt = attempt;
  });
```

---

## 🔐 Governance Enforcement

CerbiStream supports structured logging enforcement via JSON rules. If governance is enabled, logs must match the schema.

```json
{
  "LoggingProfiles": {
    "SecurityLog": {
      "RequiredFields": ["UserId", "IPAddress"],
      "OptionalFields": ["DeviceType"]
    }
  }
}
```

Use [GovernanceAnalyzer](https://www.nuget.org/packages/CerbiStream.GovernanceAnalyzer) to validate rules at build time.

---

## 📊 Telemetry Provider Support

| Provider                  | Supported? |
|--------------------------|------------|
| Azure App Insights       | ✅         |
| AWS CloudWatch           | ✅         |
| GCP Trace                | ✅         |
| Datadog                  | ✅         |
| OpenTelemetry (default)  | ✅         |

Enable them via:

```csharp
options.SetTelemetryProvider(new AppInsightsTelemetryProvider());
```

---

## 🔌 Multi-Telemetry Routing

Route different logs to different providers with governance config or custom logic:

```json
{
  "TelemetryRouting": {
    "SecurityLogs": "Azure",
    "InfraLogs": "AWS"
  }
}
```

---

## 🧠 Why Use CerbiStream?

- ✅ Structured logging & telemetry
- ✅ No PII, ML/AI friendly
- ✅ Preset modes for dev/test/benchmarks
- ✅ Multi-cloud support
- ✅ Enforced governance (optional)
- ✅ Works with `ILogger<T>`

---

📜 **License**: MIT

📣 **Want to contribute?** Star the repo ⭐, open an issue 🐛, or suggest a feature 🧠!

🧑‍💻 Created by [@Zeroshi](https://github.com/Zeroshi)

