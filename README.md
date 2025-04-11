# CerbiStream: Dev-Friendly, Governance-Enforced Logging for .NET

[![NuGet](https://img.shields.io/nuget/v/CerbiStream?style=flat-square)](https://www.nuget.org/packages/CerbiStream)
[![Downloads](https://img.shields.io/nuget/dt/CerbiStream?style=flat-square)](https://www.nuget.org/packages/CerbiStream)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=flat-square)](https://dotnet.microsoft.com/download)

[![Dev Friendly](https://img.shields.io/badge/dev--friendly-%E2%9C%94%EF%B8%8F-brightgreen?style=flat-square)](https://github.com/Zeroshi/Cerbi-CerbiStream)
[![Governance Enforced](https://img.shields.io/badge/governance-enforced-red?style=flat-square)](https://github.com/Zeroshi/Cerbi-CerbiStream)

[![Cerbi CI](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/cerbi-devsecops.yml/badge.svg?branch=master)](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/cerbi-devsecops.yml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=Zeroshi_Cerbi-CerbiStream&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Zeroshi_Cerbi-CerbiStream)

> 🚀 **[View CerbiStream Benchmarks](https://cerbi.systems)**
>
> Compare against Serilog, NLog, and others. CerbiStream is engineered for high performance, strict governance, and enterprise-grade log routing.

---

## Table of Contents

- [Overview](#overview)
- [Highlights](#highlights)
- [Features](#features)
- [Architecture & Implementation](#architecture--implementation)
- [Preset Modes and Configuration](#preset-modes-and-configuration)
- [Usage Examples](#usage-examples)
- [Integration & Supported Platforms](#integration--supported-platforms)
- [Governance and Compliance](#governance-and-compliance)
- [Telemetry Provider Support](#telemetry-provider-support)
- [Unit Testing](#unit-testing)
- [Cerbi Suite: The Bigger Picture](#cerbi-suite-the-bigger-picture)
- [Contributing](#contributing)
- [License](#license)

---

## 🔄 Recent Updates (v1.0.17)

### 🔐 Encrypted File Logging & Rotation (Fallback Mode)
- Added `EncryptedFileRotator` with support for:
  - File size & age-based rotation
  - AES encryption with configurable keys/IVs
  - Archive naming with timestamped `.enc` files
- Background task service (`EncryptedFileRotationService`) for automatic, periodic rotation
- Configurable via `FileFallbackOptions`:
  - `MaxFileSizeBytes`
  - `MaxFileAge`
  - `EncryptionKey`
  - `EncryptionIV`

### 🧪 Unit Test Coverage
- Increased to ~65.5%
- Added test coverage for:
  - Fallback logger pipeline
  - Rotation trigger behavior
  - Telemetry factory mapping

### 🏗 Upcoming Work
- FIPS toggle evaluation
- GitHub Copilot hints for governance rules

---


## Overview

**CerbiStream** is a high-performance, dev-friendly logging framework for .NET that not only emphasizes low latency and high throughput but also enforces structured logging governance. Designed for modern applications using `ILogger<T>`, CerbiStream integrates seamlessly into ASP.NET Core, Blazor, and Entity Framework projects. Its flexible configuration options and robust governance support make it ideal for both development and enterprise-scale deployments.

CerbiStream is a core component of the **Cerbi Suite**—a set of tools designed for observability, log governance, routing, and telemetry in regulated and performance-critical environments.

---

## Highlights

- **Dev-Friendly & Flexible:**  
  Works out of the box with .NET Core’s logging abstractions.

- **High Performance:**  
  Engineered for low latency and high throughput with efficient resource usage even when logs are output in realistic development modes.

- **Governance-Enforced Logging:**  
  Ensures logs conform to structured formats and compliance requirements via internal or pluggable governance validators.

- **Multiple Integration Options:**  
  Supports RabbitMQ, Kafka, Azure Service Bus, AWS SQS/Kinesis, and Google Pub/Sub.

- **Encryption & Security:**  
  Offers flexible encryption modes—including Base64 and AES—to secure sensitive logging data.

- **Queue-First Architecture:**  
  Decouples log generation from delivery, enhancing resilience and scalability via the CerbIQ routing component.

- **Telemetry & Analytics:**  
  Built-in support for telemetry providers (OpenTelemetry, Azure App Insights, AWS CloudWatch, etc.) for end-to-end observability.

---

## Features

- **Developer Modes:**  
  - **EnableDevModeMinimal():** Minimal console logging without metadata injection—perfect for lightweight development debugging.  
  - **EnableDeveloperModeWithoutTelemetry():** Console logging with metadata injection but no telemetry data.  
  - **EnableDeveloperModeWithTelemetry():** Full-fledged logging with metadata, telemetry, and governance checks.  
  - **EnableBenchmarkMode():** A silent mode disabling all outputs, enrichers, and telemetry, ideal for performance measurement.

- **Governance Enforcement:**  
  Use built-in or externally provided validators (via the CerbiStream.GovernanceAnalyzer or custom hooks) to enforce required log fields and format consistency.

- **Encryption Options:**  
  Configure encryption modes to secure log data with options for None, Base64, or AES encryption.

- **Queue-First, Sink-Agnostic Logging:**  
  Route logs through configured messaging queues first for enhanced fault tolerance and delayed sink processing via CerbIQ.

- **Telemetry Integration:**  
  Out-of-the-box support for major telemetry platforms ensures your log data is immediately useful for observability and diagnostics.

## Architecture & Implementation

CerbiStream’s architecture is designed to maximize logging performance while ensuring compliance and structured data integrity:

- **Asynchronous Processing:**  
  The logging pipeline is built using modern asynchronous patterns to avoid blocking I/O. This allows high-volume log generation without impacting application performance.

- **Queue-First Model:**  
  Log events are first enqueued (supporting various messaging systems) before being dispatched to the final sink. This decoupling reduces processing spikes and improves reliability.

- **Modular & Configurable:**  
  The framework uses a fluent API for configuration, letting you easily switch modes, enable encryption, set up telemetry, and plug in custom governance validators.

- **Governance & Validation:**  
  A key aspect of CerbiStream is its governance engine, which enforces structured logging policies. Whether integrated via CerbiStream.GovernanceAnalyzer or custom logic, it ensures that all log messages meet your organizational standards.

- **Performance Modes:**  
  Different preset modes (Benchmark, Developer Modes) allow you to choose the level of output and enrichment based on your environment—from raw performance testing to full-featured dev logging.

---

## Preset Modes and Configuration

### Developer Modes

- **EnableDevModeMinimal()**  
  Minimal logging output to the console without metadata or governance checks.
  ```csharp
  builder.Logging.AddCerbiStream(options => options.EnableDevModeMinimal());
  ```

- **EnableDeveloperModeWithoutTelemetry()**  
  Console logging with metadata injection but without telemetry.
  ```csharp
  builder.Logging.AddCerbiStream(options => options.EnableDeveloperModeWithoutTelemetry());
  ```

- **EnableDeveloperModeWithTelemetry()**  
  Full developer mode with metadata enrichment and telemetry.
  ```csharp
  builder.Logging.AddCerbiStream(options => options.EnableDeveloperModeWithTelemetry());
  ```

### Benchmark Mode

- **EnableBenchmarkMode()**  
  Disables all outputs and enrichments for pure performance testing.
  ```csharp
  builder.Logging.AddCerbiStream(options => options.EnableBenchmarkMode());
  ```

### Advanced Customization

**Queue Configuration:**
```csharp
options.WithQueue("RabbitMQ", "localhost", "logs-queue");
```

**Encryption:**
```csharp
options.WithEncryptionMode(EncryptionType.AES)
       .WithEncryptionKey(myKey, myIV);
```

**Governance Validator:**
```csharp
options.WithGovernanceValidator((profile, log) =>
{
    return log.ContainsKey("UserId") && log.ContainsKey("IPAddress");
});
```

**Feature Toggles:**
```csharp
options.WithTelemetryLogging(true)
       .WithConsoleOutput(true)
       .WithMetadataInjection(true)
       .WithGovernanceChecks(true);
```

---

## Usage Examples

**Basic Example**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddCerbiStream(options =>
{
    options.EnableDevModeMinimal();
});
var app = builder.Build();
app.Run();
```

**Developer Mode with Custom Queue and Encryption**
```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.WithQueue("Kafka", "broker-address", "app-logs")
           .WithEncryptionMode(EncryptionType.Base64)
           .EnableDeveloperModeWithoutTelemetry();
});
```

**Full Mode with Telemetry and Governance**
```csharp
builder.Logging.AddCerbiStream(options =>
{
    options
        .WithQueue("AzureServiceBus", "sb://myservicebus.servicebus.windows.net", "logs-queue")
        .WithEncryptionMode(EncryptionType.AES)
        .WithGovernanceValidator((profile, log) =>
        {
            return log.ContainsKey("UserId") && log.ContainsKey("IPAddress");
        })
        .EnableDeveloperModeWithTelemetry();
});
```

---

## File Fallback Logging with Encrypted Rotation

CerbiStream can automatically fallback to a local file logger if upstream log delivery fails. You can configure size/age-based rotation with optional AES encryption for compliance.

### Enable Fallback in Your Program.cs or Startup.cs

```csharp
builder.Logging.AddCerbiStream(options =>
{
    options.EnableDevModeMinimal(); // Or Prod/Custom config

    options.FileFallback = new FileFallbackOptions
    {
        Enable = true,
        PrimaryFilePath = "logs/primary.json",
        FallbackFilePath = "logs/fallback.json",
        RetryCount = 3,
        RetryDelay = TimeSpan.FromMilliseconds(250),
        MaxFileSizeBytes = 5 * 1024 * 1024, // Rotate at 5MB
        MaxFileAge = TimeSpan.FromMinutes(10), // Or age-based
        EncryptionKey = "your-32-char-AES-key-here!!",
        EncryptionIV = "your-16byte-iv"
    };
});
```

## Add Background Rotation Service ##

Make sure to register the rotation background service if you're using fallback:

```csharp
builder.Services.AddHostedService<EncryptedFileRotationService>();
```

---

## Integration & Supported Platforms

CerbiStream is designed to work in a variety of environments:

**Messaging Queues:**  
Supports RabbitMQ, Kafka, Azure Service Bus, AWS SQS/Kinesis, and Google Pub/Sub.

**Telemetry Providers:**

| Provider           | Supported |
|--------------------|-----------|
| OpenTelemetry      | ✅        |
| Azure App Insights | ✅        |
| AWS CloudWatch     | ✅        |
| GCP Trace          | ✅        |
| Datadog            | ✅        |

**Pluggable Sinks:**  
CerbiStream is “sink-agnostic” – logs are initially routed to queues, and you can integrate downstream tools (e.g., CerbIQ) to manage final delivery to systems such as Splunk, Elasticsearch, or Blob Storage.

---

## Governance and Compliance

A key differentiator of CerbiStream is its built-in governance:

**Structured Logging Enforcement:**  
Ensures that every log entry adheres to predefined schemas for consistency, aiding in compliance with regulatory standards (e.g., HIPAA, GDPR).

**External Governance Hook:**  
If needed, you can provide your own governance validator:
```csharp
options.WithGovernanceValidator((profile, log) =>
{
    return log.ContainsKey("UserId") && log.ContainsKey("IPAddress");
});
```

**Optional CerbiStream.GovernanceAnalyzer:**  
An add-on package that performs static code analysis to ensure that logging policies are consistently followed.

---

## Telemetry Provider Support

CerbiStream integrates seamlessly with popular telemetry systems to provide extended observability:

Supported providers include: OpenTelemetry, Azure App Insights, AWS CloudWatch, GCP Trace, and Datadog.

Configuration is straightforward through the fluent API, ensuring that enriched log data is automatically forwarded to your chosen telemetry platform.

---

## Unit Testing

Example unit test for CerbiStream logging:
```csharp
var mockQueue = Substitute.For<IQueue>();
var logger = new CerbiLoggerBuilder()
    .WithQueue("TestQueue", "localhost", "unit-test-logs")
    .UseNoOpEncryption()
    .Build(mockQueue);

var result = await logger.LogEventAsync("Test log event", LogLevel.Information);
Assert.True(result);
```

---

## Cerbi Suite: The Bigger Picture

CerbiStream is a core component of the Cerbi suite—a broader ecosystem aimed at providing enterprise-grade observability, governance, and log routing solutions. The suite includes:

- **CerbiStream:** For high-performance, governance-enforced logging.
- **CerbIQ:** For advanced log routing, aggregation, and delivery to various sinks.
- **CerbiStream.GovernanceAnalyzer:** For static and runtime validation ensuring consistent log compliance.

---

## Contributing

Contributions are welcome!

- **Report Bugs or Request Features:** Open an issue on GitHub.
- **Submit Pull Requests:** Follow our code style guidelines and ensure tests pass.
- **Join the Community:** Star the repo, share feedback, and help improve CerbiStream.

---

## License

This project is licensed under the MIT License.

---

Star the repo ⭐ | Contribute 🔧 | File issues 🐛  
Created by @Zeroshi
