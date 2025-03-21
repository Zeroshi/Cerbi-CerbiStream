# CerbiStream Logging Library

CerbiStream is a **next-generation** logging solution built for **structured logs, governance enforcement, and multi-destination routing**. It ensures secure, consistent, and high-performance logging for **cloud, on-prem, and hybrid environments**.

---

## 🚀 What's New?  
- **Telemetry Support** – Seamless integration with **AWS CloudWatch, GCP Cloud Trace, Azure Application Insights, and Datadog** for distributed tracing.  
- **Configurable Telemetry Providers** – Choose **which telemetry provider to use** or disable telemetry entirely.  
- **Optimized Telemetry** – Exclude noisy events (`DebugLog`, `HealthCheck`, etc.) and control **sampling rates** for cost optimization.  
- **Multi-Cloud Telemetry** – Route logs and traces to **multiple cloud providers** based on your architecture.  

---


If you want Governance Enforcement, also install:

dotnet add package CerbiStream.GovernanceAnalyzer


## 📦 Installation  

Install **CerbiStream** from NuGet:  

```sh
dotnet add package CerbiStream
```


## 📦 Installation

Install **CerbiStream** from NuGet:

```sh
dotnet add package CerbiStream
```
If you want Governance Enforcement, also install:

```sh
dotnet add package CerbiStream.GovernanceAnalyzer
```
⚡ Quick Start (Minimal Setup)
With CerbiStream, you can integrate logging in seconds.

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CerbiStream.Logging.Extensions;

```csharp
class Program
{
    static void Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddCerbiStream(options =>
                {
                    options.SetQueue("RabbitMQ", "localhost", "logs-queue");
                    options.EnableDevMode();
                    options.EnableGovernance();
                });
            })
            .BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Application started successfully!");
        logger.LogError("This is a test error log.");
        logger.LogWarning("Potential issue detected.");
        logger.LogCritical("Critical failure occurred!");
    }
}
```

🛠️ Advanced Configuration

If you need more control, you can configure CerbiStream dynamically.

```csharp
var config = new CerbiStreamOptions();
config.SetQueue("Kafka", "kafka://broker-url", "app-logs");
config.DisableDevMode();
config.EnableGovernance();
config.IncludeAdvancedMetadata();

var logger = new CerbiStreamLogger(config);
```
## 🌐 Supported Logging Destinations

| Queue Type         | Example Usage        |
|--------------------|---------------------|
| **RabbitMQ**       | `QueueType = "RabbitMQ"` |
| **Kafka**         | `QueueType = "Kafka"` |
| **Azure Queue Storage** | `QueueType = "AzureQueue"` |
| **Azure Service Bus**   | `QueueType = "AzureServiceBus"` |
| **AWS SQS**       | `QueueType = "AWS_SQS"` |
| **AWS Kinesis**   | `QueueType = "AWS_Kinesis"` |
| **Google Pub/Sub** | `QueueType = "GooglePubSub"` |

---

## 🔍 Automatic Metadata (No Setup Required)

| Metadata Field        | Auto-Detected? | Example Value         |
|-----------------------|---------------|----------------------|
| **CloudProvider**     | ✅ Yes        | AWS, Azure, GCP, On-Prem |
| **Region**           | ✅ Yes        | us-east-1, eu-west-2 |
| **Environment**      | ✅ Yes        | Development, Production |
| **ApplicationVersion** | ✅ Yes        | v1.2.3 |
| **RequestId**        | ✅ Yes (Generated) | abc123 |
| **TransactionType**  | ❌ Developer Sets | REST, gRPC, Kafka |
| **TransactionStatus** | ❌ Developer Sets | Success, Failed |

---

## 🔐 Governance & Structured Logging

Governance allows organizations to enforce **structured logging signatures** by:
- ✅ **Defining required fields** (e.g., every log must include `UserId`, `RequestId`, etc.).
- ✅ **Allowing optional fields** that developers can extend dynamically.
- ✅ **Using a governance configuration file** (`cerbi_governance.json`) for dynamic updates.
Example Governance JSON:
```csharp
{
  "LoggingProfiles": {
    "TransactionLog": {
      "RequiredFields": ["TransactionId", "UserId", "Amount"],
      "OptionalFields": ["DiscountCode"]
    },
    "SecurityLog": {
      "RequiredFields": ["UserId", "IPAddress"],
      "OptionalFields": ["DeviceType"]
    }
  }
}
```
If GovernanceEnabled = true, logs must match the configured structure.

✅ Governance Analyzer (Build-Time Validation)

CerbiStream GovernanceAnalyzer uses Roslyn to validate log compliance at build time.
This ensures structured logs without runtime overhead.

🛠 Debug Mode (Local Development)
CerbiStream prevents queue logging while debugging.
This is enabled by default (EnableDevMode = true).
```csharp
var config = new CerbiStreamOptions();
config.EnableDevMode();

var logger = new CerbiStreamLogger(config);
await logger.LogEventAsync("Debugging locally", LogLevel.Debug);
```
📊 Meta Data Sharing (Opt-In)

CerbiStream collects aggregate trends across applications for AI-powered insights.
✅ No Personally Identifiable Information (PII) is stored.

If enabled, your logs contribute to global analytics (Error Trends, Cloud Performance, API Response Issues).
If disabled, your logs remain 100% private.

```csharp
var config = new CerbiStreamOptions();
config.IncludeAdvancedMetadata();
config.IncludeSecurityMetadata();
```

## 📊 Telemetry Support (Optional)
CerbiStream now supports **distributed tracing and application performance monitoring** through multiple telemetry providers.

### **Supported Telemetry Providers**
| Provider                      | Status       |
|--------------------------------|-------------|
| **Azure Application Insights** | ✅ Supported |
| **AWS CloudWatch**             | ✅ Supported |
| **Google Cloud Trace**         | ✅ Supported |
| **Datadog**                    | ✅ Supported |
| **OpenTelemetry** (default)    | ✅ Supported |

---

## 🛠 **Configuring Telemetry in CerbiStream**
To enable telemetry, **specify a provider** in the `CerbiStreamOptions` configuration:

```csharp
var config = new CerbiStreamOptions();
config.SetTelemetryProvider(new AppInsightsTelemetryProvider()); // Choose from AppInsights, AWS, GCP, Datadog, etc.
config.SetQueue("RabbitMQ", "localhost", "logs-queue");
config.EnableGovernance();

var logger = new CerbiStreamLogger(config);
```

## 🌍 Multi-Cloud Telemetry Routing  
You can route different types of logs to different telemetry providers for better visibility.

| **Log Type**               | **Default Destination**         |
|----------------------------|--------------------------------|
| **Application Logs**       | Google Cloud Trace            |
| **Infrastructure Logs**    | AWS CloudWatch                |
| **Security & Audit Logs**  | Azure Application Insights    |
| **Performance Metrics**    | Datadog                        |

To customize this, configure the routing rules in your governance JSON file:

```csharp
{
  "TelemetryRouting": {
    "ApplicationLogs": "GoogleCloud",
    "InfraLogs": "AWS",
    "SecurityLogs": "Azure",
    "PerformanceMetrics": "Datadog"
  }
}
```

## ⚡ Optimized Telemetry Collection  
CerbiStream minimizes unnecessary logging noise while ensuring critical events are captured.  

- ✅ **Event Sampling** – Configurable rate limiting to balance cost & observability.  
- ✅ **Noise Reduction** – Filters out low-priority logs like `HealthCheck` & `DebugLog`.  
- ✅ **Auto-Enabled for Supported Providers** – Telemetry is automatically enabled when a supported provider is detected (AWS, GCP, Azure).  

## 🔄 Auto-Enabled Telemetry Providers  
CerbiStream detects and configures telemetry based on your cloud environment.

| **Cloud Provider** | **Auto-Enabled Telemetry Service**   |
|--------------------|--------------------------------------|
| **AWS**           | CloudWatch                           |
| **Azure**         | Application Insights                |
| **Google Cloud**  | Stackdriver Trace                   |
| **On-Prem**       | OpenTelemetry (Custom Configuration) |

Developers can override these settings to manually specify their preferred telemetry provider.  

## 🔌 Multi-Telemetry Provider Setup  
CerbiStream allows you to integrate **multiple telemetry providers** for better observability across cloud environments.  

### 🚀 Example: Using OptimizedTelemetryProvider, AWS CloudWatch & Azure Application Insights  

```csharp
using CerbiStream.Configuration;
using CerbiStream.Interfaces;
using CerbiStream.Classes.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static void Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddCerbiStream(options =>
                {
                    options.SetQueue("RabbitMQ", "localhost", "logs-queue");
                    options.EnableDevMode();
                    options.EnableGovernance();

                    // ✅ Optimized telemetry provider (efficient tracing with sampling)
                    options.AddTelemetryProvider(new OptimizedTelemetryProvider(samplingRate: 0.5));  

                    // ✅ Add multiple telemetry providers
                    options.AddTelemetryProvider(new CloudWatchTelemetryProvider());  
                    options.AddTelemetryProvider(new AppInsightsTelemetryProvider());  
                });
            })
            .BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Application started successfully!");
        logger.LogError("Critical error detected in system.");
    }
}
```

You can enable, disable, or prioritize telemetry providers as needed.
```csharp

options.EnableTelemetry(); // Enable all auto-detected telemetry providers  
options.DisableTelemetry(); // Disable all telemetry tracking  
options.SetTelemetrySamplingRate(0.3); // 30% sampling for cost optimization  
```

Example: Using OptimizedTelemetryProvider

```csharp
options.AddTelemetryProvider(new OptimizedTelemetryProvider(samplingRate: 0.5));
```

-------------

## 📈 Rollup & Multi-Project Visibility

CerbiStream supports **centralized telemetry aggregation**, allowing you to visualize logs, metrics, and traces from **multiple services or microservices** in one place.

This is especially useful for:

- 🚀 **Microservices Architectures**  
- 🧩 **Distributed Systems**  
- 🛠️ **Multi-Environment Monitoring (Dev / QA / Prod)**

### 🧭 Example: Using Application Insights for Rollups

With Azure Application Insights, all telemetry (from different apps) can be **grouped under a single Application Map**:

```csharp
options.AddTelemetryProvider(new AppInsightsTelemetryProvider());
```

Be sure to:

Use the same Instrumentation Key or Connection String across services.
Tag logs with AppName, Environment, or Component for grouping:

```csharp
var metadata = new Dictionary<string, string>
{
    { "AppName", "CheckoutService" },
    { "Environment", "Production" },
    { "Component", "PaymentProcessor" }
};

logger.LogEvent("Payment failed", LogLevel.Error, metadata);
```


-----


## 🔥 Why Use CerbiStream?

- ✅ **No External Dependencies** – Just install & start logging.
- 🚀 **Optimized Performance** – Logs lightweight metadata automatically.
- 🔒 **Security First** – Encrypts fields and ensures **NPI-free** logging.
- 🌍 **Global Insights** – Identify patterns across industries *(if opted-in)*.
- ⚡ **Minimal Setup** – Works **out-of-the-box** with simple constructor injection.


📜 License
CerbiStream is open-source and available under the MIT License.
