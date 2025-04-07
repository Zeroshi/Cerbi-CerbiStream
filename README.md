# CerbiStream: Dev-Friendly Logging for .NET

![NuGet](https://img.shields.io/nuget/v/CerbiStream?style=flat-square)
![Downloads](https://img.shields.io/nuget/dt/CerbiStream?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=flat-square)

![Dev Friendly](https://img.shields.io/badge/dev--friendly-%E2%9C%94%EF%B8%8F-brightgreen?style=flat-square)
![Governance Enforced](https://img.shields.io/badge/governance-enforced-red?style=flat-square)

[![Cerbi CI](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/cerbi-devsecops.yml/badge.svg?branch=master)](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/cerbi-devsecops.yml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=Zeroshi_Cerbi-CerbiStream&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Zeroshi_Cerbi-CerbiStream)

> 🚀 **[View CerbiStream Benchmarks](https://cerbi.systems)**
>
> Compare against Serilog, NLog, and others. CerbiStream is tuned for performance, governance, and enterprise-scale routing.

---

## ✅ Highlights

- Works with `ILogger<T>` out of the box
- Structured logging enforcement via CerbiStream or GovernanceAnalyzer
- Fully supports RabbitMQ, Kafka, Azure Service Bus, AWS SQS/Kinesis, GCP Pub/Sub
- Flexible encryption: None, Base64, AES (configurable)
- Optional Roslyn-based GovernanceAnalyzer or external validator hook
- 🔁 Queue-first architecture (sink-agnostic, logs route through CerbIQ if desired)
- Entity Framework and Blazor-friendly via external governance validator option


---

🔄 External Governance Hook
If you're not using the CerbiStream.GovernanceAnalyzer package (e.g., to avoid Roslyn dependency issues with Entity Framework or Blazor), you can provide your own governance validation logic:
```csharp
options.WithGovernanceValidator((profile, data) =>
{
    // Custom governance validation logic
    return data.ContainsKey("UserId") && data.ContainsKey("IPAddress");
});
```

This lets you enforce structure without referencing Roslyn, making CerbiStream fully compatible with EF Core and other analyzers.

---

## ✨ What's New in v1.0.11

- New presets: `BenchmarkMode()`, `EnableDeveloperModeWithTelemetry()`
- Toggle: telemetry, console, metadata, governance
- JSON conversion with encryption
- Queue routing using enums

---

## 🧰 Install

```bash
dotnet add package CerbiStream
```
Optional governance analyzer:
```bash
dotnet add package CerbiStream.GovernanceAnalyzer
```

---

## ⚡ Quick Start

```csharp
builder.Logging.AddCerbiStreamWithRouting(options =>
{
    options.WithQueue("RabbitMQ", "localhost", "logs-queue")
           .EnableDeveloperModeWithoutTelemetry()
           .WithEncryptionMode(EncryptionType.Base64);
});
```

---

# Configuration Setup Guide

CerbiStream is a structured logging framework designed for observability, telemetry enrichment, and governance enforcement. This guide demonstrates how to configure `CerbiStreamOptions` using the available setup methods.

## 🔧 Basic Setup

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.EnableDevModeMinimal(); // Logs only to console, minimal metadata
});
```

## ⚙️ Available Preset Modes

### ✅ `EnableDevModeMinimal()`
Minimal output, console only, no metadata injection, ideal for simple development scenarios.
```csharp
options.EnableDevModeMinimal();
```

### ✅ `EnableDeveloperModeWithoutTelemetry()`
Includes basic metadata injection but skips telemetry logging.
```csharp
options.EnableDeveloperModeWithoutTelemetry();
```

### ✅ `EnableDeveloperModeWithTelemetry()`
Enables metadata injection, console output, and sends to telemetry.
```csharp
options.EnableDeveloperModeWithTelemetry();
```

### ✅ `EnableBenchmarkMode()`
Disables all outputs and features for benchmarking.
```csharp
options.EnableBenchmarkMode();
```

## 🛠 Custom Configuration

### Set Custom Queue
```csharp
options.WithQueue("RabbitMQ", "localhost", "my-logs");
```

### Set Encryption Mode
```csharp
options.WithEncryptionMode(EncryptionType.Base64);
options.WithEncryptionKey(keyBytes, ivBytes);
```

### Enable or Disable Features
```csharp
options.WithTelemetryLogging(true);
options.WithConsoleOutput(true);
options.WithMetadataInjection(true);
options.WithTelemetryEnrichment(true);
options.WithGovernanceChecks(true);
options.WithDisableQueue(false);
```

## 🧠 Add Advanced Metadata

```csharp
options.WithAdvancedMetadata(true);
options.WithSecurityMetadata(true);
```

## 🧪 External Governance Validator

```csharp
options.WithGovernanceValidator((profile, log) =>
{
    // Custom validation logic
    return log.ContainsKey("requiredKey");
});
```

## 🔍 Mode Detection
You can check the runtime mode using:
```csharp
bool isMinimal = options.IsMinimalMode;
bool isBenchmark = options.IsBenchmarkMode;
```

---

📌 **Note:** These methods are chainable, allowing fluent configuration:

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.WithQueue("RabbitMQ", "localhost", "audit-logs")
           .WithConsoleOutput(true)
           .WithGovernanceChecks(true);
});
```


### 🔹 `EnableDevModeMinimal()`
**Purpose:** Quickly enables console logging with minimal features for local development or container diagnostics.
- ✅ Console Output: `true`
- ❌ Telemetry Enrichment: `false`
- ❌ Metadata Injection: `false`
- ❌ Governance Checks: `false`
- ❌ Queue Sending: `enabled`
- ✅ Best for: Minimal test containers, low-overhead logging in dev

**Example:**
```csharp
builder.Logging.AddCerbiStream(options => options.EnableDevModeMinimal());
```


### 🔹 `EnableDeveloperModeWithoutTelemetry()`
**Purpose:** Enables local developer logging without external telemetry.
- ✅ Console Output: `true`
- ✅ Metadata Injection: `true`
- ❌ Telemetry Enrichment: `false`
- ❌ Governance Checks: `false`

**Example:**
```csharp
builder.Logging.AddCerbiStream(options => options.EnableDeveloperModeWithoutTelemetry());
```


### 🔹 `EnableDeveloperModeWithTelemetry()`
**Purpose:** Enables all developer logging features including telemetry, for full context during dev work.
- ✅ Console Output: `true`
- ✅ Metadata Injection: `true`
- ✅ Telemetry Enrichment: `true`
- ❌ Governance Checks: `false`

**Example:**
```csharp
builder.Logging.AddCerbiStream(options => options.EnableDeveloperModeWithTelemetry());
```


### 🔹 `EnableBenchmarkMode()`
**Purpose:** Disables all overhead logging features, ideal for performance benchmarking.
- ❌ Console Output: `false`
- ❌ Metadata Injection: `false`
- ❌ Telemetry Enrichment: `false`
- ❌ Governance Checks: `false`
- ✅ Queue Sending: `disabled`

**Example:**
```csharp
builder.Logging.AddCerbiStream(options => options.EnableBenchmarkMode());
```

---

## 🔐 Runtime Encryption

```csharp
options.WithEncryptionMode(EncryptionType.AES)
       .WithEncryptionKey(myKey, myIV);
```

Default (lazy) test keys:
```csharp
var (key, iv) = EncryptionHelpers.GetInsecureDefaultKeyPair();
options.WithEncryptionKey(key, iv);
```

KeyVault example:
```csharp
var key = Convert.FromBase64String(await secretClient.GetSecret("CerbiKey"));
var iv = Convert.FromBase64String(await secretClient.GetSecret("CerbiIV"));
```

---

## 🛠️ Preset Modes

| Method                                 | Description                                   |
|----------------------------------------|-----------------------------------------------|
| `EnableDeveloperModeWithTelemetry()`   | Console + telemetry + metadata                |
| `EnableDeveloperModeWithoutTelemetry()`| Console + metadata, no telemetry              |
| `EnableDevModeMinimal()`               | Console only                                  |
| `EnableBenchmarkMode()`                | Silent mode (no telemetry, queue, or console) |

---

## 🔁 Retry Example

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

## 🔧 Configuration Options

| Option                      | Description                                      |
|-----------------------------|--------------------------------------------------|
| `.WithQueue(...)`          | Configure queue host, name, and type            |
| `.DisableQueue()`          | Stops sending logs to queues                    |
| `.WithTelemetryProvider()` | Set custom telemetry provider                   |
| `.IncludeSecurityMetadata()`| Adds IP/UserID info                             |
| `.EnableTelemetryLogging()`| Sends to telemetry even if queue is disabled    |

---

## 📊 Telemetry Provider Support

| Provider          | Supported |
|-------------------|-----------|
| OpenTelemetry     | ✅        |
| Azure App Insights| ✅        |
| AWS CloudWatch    | ✅        |
| GCP Trace         | ✅        |
| Datadog           | ✅        |

---

## 📘 Code Samples

### CerbiLoggerBuilder
```csharp
var logger = new CerbiLoggerBuilder()
    .UseAzureServiceBus("<conn>", "<queue>")
    .EnableDebugMode()
    .Build(logger, new ConvertToJson(), new NoOpEncryption());
```

### Fluent API
```csharp
var options = new CerbiStreamOptions()
    .WithEncryptionMode(EncryptionType.Base64)
    .WithQueue("RabbitMQ", "localhost", "logs");
```

### AddCerbiStreamWithRouting (DI)
```csharp
builder.Logging.AddCerbiStreamWithRouting(options =>
{
    options.WithQueue("AzureServiceBus", "sb://...", "queue")
           .WithEncryptionMode(EncryptionType.AES);
});
```

---

## 🧪 Unit Test Example

```csharp
var mockQueue = Substitute.For<IQueue>();
var logger = new Logging(Substitute.For<ILogger<Logging>>(), mockQueue, new ConvertToJson(), new NoOpEncryption());

var result = await logger.LogEventAsync("Test", LogLevel.Information);
Assert.True(result);
```

---

## 🌐 Supported Queues

- RabbitMQ
- Kafka
- Azure Queue / Service Bus
- AWS SQS / Kinesis
- Google Pub/Sub

---

## 🧵 Queue-First Logging (Sink-Agnostic)

CerbiStream **does not directly send logs to sinks** like Splunk, Elastic, or Blob.

Instead:
- 🔁 Logs are emitted to **queues only** (Kafka, RabbitMQ, Azure, etc.)
- 🧠 CerbIQ reads from these queues and sends logs to sinks
- ✅ Keeps log generation decoupled from log delivery

This design gives you:
- Better performance
- Retry-friendly resilience
- Pluggable downstream integrations

➡️ Add CerbIQ to handle routing and sink delivery.

---

## 🔐 Governance Enforcement (Optional)

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

---

## 📜 License

MIT

---

Star the repo ⭐ — Contribute 🔧 — File issues 🐛

Created by [@Zeroshi](https://github.com/Zeroshi)
