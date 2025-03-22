# CerbiStream Logging Library

CerbiStream is a **next-generation** logging solution built for **structured logs, governance enforcement, and multi-destination routing**. It ensures secure, consistent, and high-performance logging for **cloud, on-prem, and hybrid environments**.

---

## 🚀 What's New?  
- **Telemetry Context Enrichment** – Automatically include metadata like `ServiceName`, `OriginApp`, `UserType`, `Feature`, `IsRetry`, and `RetryAttempt`.
- **Static Enrichment** – All telemetry context fields are set once and injected into logs automatically.
- **Retry Metadata** – Integrated with Polly and middleware to track retries at the log level.
- **Telemetry Support** – Seamless integration with **AWS CloudWatch, GCP Cloud Trace, Azure Application Insights, and Datadog**.
- **Configurable Telemetry Providers** – Easily plug in multiple providers.
- **Optimized Telemetry** – Exclude noisy events (e.g., `DebugLog`, `HealthCheck`) and enable **sampling** for cost control.

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

            // Set once, reused for all logs
            TelemetryContext.ServiceName = "CheckoutService";
            TelemetryContext.OriginApp = "MyFrontendApp";
            TelemetryContext.UserType = "InternalUser"; // System | ApiConsumer | Guest
        });
    })
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("App started");
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

## ✅ Telemetry Context Fields (Auto-Enriched)

- Field	Description
- ServiceName	- Logical name of the service
- OriginApp	- Source app triggering the log
- UserType - System, ApiConsumer, etc.
- Feature -	Business context like Checkout
- IsRetry -	true if retrying the operation
- RetryAttempt - Number of retry attempts

---

## 🧩 Feature & Business Area Enum

Use a shared enum for consistency:

```csharp
Always show details

Copy
public enum FeatureArea
{
    Checkout,
    Login,
    Search,
    DataExport,
    Onboarding
}
```
Set it before logging:
```csharp   
TelemetryContext.Feature = FeatureArea.Checkout.ToString();
logger.LogInformation("Item added to cart");
```

---

## 🔁 Retry Metadata (e.g., Polly Integration)

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

---


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

## 🧠 Global Observability (Optional) (coming soon)
With IncludeAdvancedMetadata(), your logs can contribute (without PII) to:

Industry-wide error trends

ML-driven root cause patterns

Performance benchmarking across cloud platforms

```csharp
config.IncludeAdvancedMetadata();
```


---

## 🔥 Why Use CerbiStream?

## 🔥 Why Use CerbiStream?

- ✅ **Structured Logs by Default** – Consistent schema with contextual metadata like `Feature`, `ServiceName`, and `RetryAttempt`.
- ✅ **Multi-Cloud Ready** – Route telemetry to Azure, AWS, GCP, Datadog, or OpenTelemetry.
- ✅ **NPI-Free Insights** – Built from the ground up to exclude personally identifiable information.
- ✅ **Business-Aware Logging** – Capture `UserType`, `OriginApp`, and `FeatureArea` for analytics and ML without leaking sensitive data.
- ✅ **Central Rollups Across Microservices** – Logs can be grouped by service, app, or feature to enable intelligent visualization and trend detection.
- ✅ **No External Dependencies** – Just install & start logging.
- 🚀 **Optimized Performance** – Uses static enrichment and telemetry sampling to reduce overhead.
- 🔒 **Security First** – Optional field-level encryption and governance enforcement.
- 🌍 **Global Insights** – Enables anonymized, cross-client trend discovery (if opted-in).
- ⚡ **Minimal Setup** – Works out-of-the-box with simple constructor injection.


📜 License
CerbiStream is open-source and available under the MIT License.
