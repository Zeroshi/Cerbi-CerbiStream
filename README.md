# CerbiStream Logging Library

![NuGet](https://img.shields.io/nuget/v/CerbiStream?style=flat-square)
![NuGet Downloads](https://img.shields.io/nuget/dt/CerbiStream?style=flat-square)
![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=flat-square)
![Dev Friendly](https://img.shields.io/badge/dev--friendly-%E2%9C%94%EF%B8%8F-brightgreen?style=flat-square)
![Governance Enforced](https://img.shields.io/badge/governance-enforced-red?style=flat-square)
![CI/CD](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/dotnet.yml/badge.svg?style=flat-square)
![Maintainability](https://sonarcloud.io/api/project_badges/measure?project=Zeroshi_Cerbi-CerbiStream&metric=sqale_rating)
![Reliability](https://sonarcloud.io/api/project_badges/measure?project=Zeroshi_Cerbi-CerbiStream&metric=reliability_rating)
![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=Zeroshi_Cerbi-CerbiStream&metric=security_rating)
![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=Zeroshi_Cerbi-CerbiStream&metric=vulnerabilities)
![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=Zeroshi_Cerbi-CerbiStream&metric=alert_status)

CerbiStream is a **next-generation** logging solution built for **structured logs, governance enforcement, and multi-destination routing**. It ensures secure, consistent, and high-performance logging for **cloud, on-prem, and hybrid environments**.

---

## ✨ What's New?

CerbiStream v1.0.11 introduces null-queue benchmarking, optimized log routing, and full developer-selectable encryption.

### 🔧 Major Improvements
- **New Preset Configuration Modes** – Developer, Minimal, and Benchmark presets.
- **Benchmark Optimization** – `BenchmarkMode()` disables queue sends.
- **Fine-Grained Controls** – Toggle telemetry, metadata, governance, and more.
- **Telemetry Separation** – `EnableTelemetryLogging()` for decoupled insights.
- **Custom Encryption Modes** – Choose from `None`, `Base64`, or `AES` for data security.

---

## 🧰 Getting Started

1. Install NuGet package
2. Configure your log mode & queue
3. Start logging with `ILogger<T>`

```sh
dotnet add package CerbiStream
```

Governance Analyzer (optional):

```sh
dotnet add package CerbiStream.GovernanceAnalyzer
```

---

## 🧠 Developer-Configurable Encryption

CerbiStream now supports runtime-configurable encryption:

```csharp
options.WithEncryptionMode(EncryptionType.AES); // Options: None, Base64, AES
```

Encryption is applied to sensitive fields like `APIKey`, `UserData`, etc.

---

## 🚀 Encryption Performance Notes

| Mode     | Performance Impact | Security Strength     |
|----------|---------------------|------------------------|
| None     | ✨ Fastest          | ❌ None                |
| Base64   | ⚡ Ultra low        | ⚠ Obfuscation only     |
| AES      | ⚡⚡ Medium          | ✅ Strong (symmetric)   |

Use `None` for benchmarks, `Base64` for minimal overhead, or `AES` for production-level security.

---

## 🛠️ Preset Config Modes

| Method                                | Description                                                              |
|--------------------------------------|--------------------------------------------------------------------------|
| `EnableDeveloperModeWithTelemetry()` | Console + metadata + telemetry (for dev/test)                            |
| `EnableDeveloperModeWithoutTelemetry()` | Console + metadata, no telemetry (clean dev logs)                       |
| `EnableDevModeMinimal()`             | Console only (no metadata or telemetry) for benchmarks or POCs           |
| `EnableBenchmarkMode()`              | All silent — disables output, telemetry, metadata, governance, and queue |

---

## 🔧 Configuration Options

| Option                        | Description                                                             |
|-------------------------------|-------------------------------------------------------------------------|
| `DisableConsoleOutput()`      | Prevents console logs                                                   |
| `DisableTelemetryEnrichment()`| Disables telemetry metadata enrichment                                  |
| `DisableMetadataInjection()`  | Skips common metadata tagging                                           |
| `DisableGovernanceChecks()`   | Turns off schema validation                                             |
| `DisableQueue()`              | Blocks all outbound log routing                                         |
| `IncludeAdvancedMetadata()`   | Adds region, cloud, and environment                                     |
| `IncludeSecurityMetadata()`   | Adds user/IP/security context                                           |
| `SetTelemetryProvider()`      | Inject custom telemetry source                                          |
| `EnableTelemetryLogging()`    | Sends telemetry even when queue is disabled                             |
| `WithEncryptionMode()`        | Choose `None`, `Base64`, or `AES` encryption                            |

---

## ⚡ Quick Start

```csharp
builder.AddLogging(cfg => cfg.AddCerbiStream(options =>
{
    options.SetQueue("RabbitMQ", "localhost", "logs-queue")
           .EnableDeveloperModeWithoutTelemetry()
           .WithEncryptionMode(EncryptionType.Base64);

    TelemetryContext.ServiceName = "UserService";
    TelemetryContext.OriginApp = "WebApp";
    TelemetryContext.UserType = "Internal";
}));
```

---

## 📉 Code Example: Encryption Factory

```csharp
IEncryption encryption = EncryptionFactory.GetEncryption(options);
string encrypted = encryption.Encrypt("sensitive");
```

---

## 🌐 Supported Queues

| Queue Type           | Example Value       |
|----------------------|---------------------|
| RabbitMQ             | "RabbitMQ"          |
| Kafka                | "Kafka"             |
| Azure Queue Storage  | "AzureQueue"        |
| Azure Service Bus    | "AzureServiceBus"   |
| AWS SQS              | "AWS_SQS"           |
| AWS Kinesis          | "AWS_Kinesis"       |
| Google Pub/Sub       | "GooglePubSub"      |

---

## 🔍 Auto-Detected Fields

| Field              | Auto? | Example       |
|--------------------|--------|---------------|
| CloudProvider      | ✅     | Azure         |
| Region             | ✅     | us-east-1     |
| InstanceId         | ✅     | WebNode-42     |
| ApplicationVersion | ✅     | v1.2.3        |
| RequestId          | ✅     | abc123        |

---

## 📊 Telemetry Provider Support

| Provider                 | Supported? |
|--------------------------|------------|
| Azure App Insights       | ✅         |
| AWS CloudWatch           | ✅         |
| GCP Trace                | ✅         |
| Datadog                  | ✅         |
| OpenTelemetry (default)  | ✅         |

```csharp
options.SetTelemetryProvider(new OpenTelemetryProvider());
```

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

CerbiStream supports schema validation through GovernanceAnalyzer:

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

Add with:
```sh
dotnet add package CerbiStream.GovernanceAnalyzer
```

---

## 🧠 Why Use CerbiStream?

- ✅ Structured, secure logging
- ✅ Supports `ILogger<T>`
- ✅ Fast + Configurable + Enforced
- ✅ ML-friendly metadata
- ✅ Works across cloud/on-prem
- ✅ Pluggable telemetry & encryption

---

📜 **License**: MIT

📣 **Want to contribute?** Star the repo ⭐, open an issue 🐛, or suggest a feature 🧠!

🧑‍💻 Created by [@Zeroshi](https://github.com/Zeroshi)
