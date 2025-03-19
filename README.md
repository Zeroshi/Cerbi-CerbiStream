# CerbiStream Logging Library

CerbiStream is a **next-generation** logging solution built for **structured logs, governance enforcement, and multi-destination routing**. It ensures secure, consistent, and high-performance logging for **cloud, on-prem, and hybrid environments**.

## 🚀 What's New?
- **Governance Enforcement** – Define and enforce structured logging standards across teams.
- **Fluid API Setup** – No complex configurations, just pass details during initialization.
- **Plug-and-Play Cloud Detection** – Auto-detects environment (AWS, Azure, GCP, On-Prem).
- **Dev Mode** – Prevents logs from being sent to external queues while debugging.
- **Secure & NPI-Free Data Collection** – Captures useful metadata without storing sensitive user data.
- **Governance Analyzer** – Uses **Roslyn** to validate logs at **build time**, improving performance.

## 📦 Installation

Install **CerbiStream** from NuGet:

dotnet add package CerbiStream
If you want Governance Enforcement, also install:


dotnet add package CerbiStream.GovernanceAnalyzer
⚡ Quick Start (Minimal Setup)
With CerbiStream, you can integrate logging in seconds.


using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CerbiStream.Logging.Extensions;

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

🛠️ Advanced Configuration

If you need more control, you can configure CerbiStream dynamically.

var config = new CerbiStreamOptions();
config.SetQueue("Kafka", "kafka://broker-url", "app-logs");
config.DisableDevMode();
config.EnableGovernance();
config.IncludeAdvancedMetadata();

var logger = new CerbiStreamLogger(config);

Here are your tables formatted neatly:

### 🌐 **Supported Logging Destinations**

| **Queue Type**          | **Example Usage**            |
|-------------------------|-----------------------------|
| **RabbitMQ**            | `QueueType = "RabbitMQ"`    |
| **Kafka**               | `QueueType = "Kafka"`       |
| **Azure Queue Storage** | `QueueType = "AzureQueue"`  |
| **Azure Service Bus**   | `QueueType = "AzureServiceBus"` |
| **AWS SQS**            | `QueueType = "AWS_SQS"`      |
| **AWS Kinesis**        | `QueueType = "AWS_Kinesis"`  |
| **Google Pub/Sub**     | `QueueType = "GooglePubSub"` |


### 🔍 **Automatic Metadata (No Setup Required)**

| **Metadata Field**       | **Auto-Detected?**        | **Example Value**           |
|-------------------------|-------------------------|----------------------------|
| **CloudProvider**       | ✅ Yes                   | AWS, Azure, GCP, On-Prem   |
| **Region**             | ✅ Yes                   | us-east-1, eu-west-2       |
| **Environment**        | ✅ Yes                   | Development, Production    |
| **ApplicationVersion** | ✅ Yes                   | v1.2.3                     |
| **RequestId**         | ✅ Yes (Generated)       | abc123                     |
| **TransactionType**   | ❌ Developer Sets        | REST, gRPC, Kafka          |
| **TransactionStatus** | ❌ Developer Sets        | Success, Failed            |

Let me know if you need further formatting adjustments! 🚀

🔐 Governance & Structured Logging

Governance allows organizations to enforce structured logging signatures.

✔ Enforce Required Fields (e.g., every log must include UserId, RequestId, etc.).
✔ Allow Optional Fields (Developers can extend the logs dynamically).
✔ Flexible Governance (Uses cerbi_governance.json for dynamic policy updates).

### 📜 Example Governance JSON

```json
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


🔹 **How It Works:**
- The JSON starts with **`{`** and ends with **`}`**.
- The JSON is **properly nested** (all opening braces have a closing pair).
- The code block starts with **\`\`\`json** and ends with **\`\`\`**.

Now, when rendered in Markdown, the JSON will be properly formatted and syntax-highlighted. 🚀


If GovernanceEnabled = true, logs must match the configured structure.

✅ Governance Analyzer (Build-Time Validation)

CerbiStream GovernanceAnalyzer uses Roslyn to validate log compliance at build time.
This ensures structured logs without runtime overhead.

## 🛠 Debug Mode (Local Development)

CerbiStream prevents queue logging while debugging.  
This is enabled by default (`EnableDevMode = true`).

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

## 🔥 Why Use CerbiStream?

- ✔ **No External Dependencies** – Just install & log.
- ✔ **Optimized Performance** – Logs lightweight metadata automatically.
- ✔ **Security First** – Encrypts fields, ensures NPI-free logging.
- ✔ **Global Insights** – See patterns across industries _(if opted-in)_.
- ✔ **Minimal Setup** – Works out-of-the-box with simple constructor injection.


📜 License
CerbiStream is open-source and available under the MIT License.
