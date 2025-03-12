# CerbiStream Logging Library

CerbiStream provides a seamless, low-config logging solution that integrates directly into your app with minimal setup. It supports structured logs, queue-based log routing, governance enforcement, and optional metadata sharing to improve observability across cloud and on-prem environments.

## What's New?
- **Fluid API Setup** – No complex configurations, just pass details during initialization.
- **Governance Enforcement** – Define and enforce structured logging standards across teams.
- **Plug-and-Play Cloud Detection** – Auto-detects environment (AWS, Azure, GCP, On-Prem).
- **No External Dependencies Required** – Handles queue setup internally.
- **Dev Mode** – Prevents logs from being sent to external queues while debugging.
- **Secure & NPI-Free Data Collection** – Captures useful metadata without storing sensitive user data.

## Quick Start (Minimal Setup)

With the Fluid API, you only need to inject CerbiStream into your app.

```csharp
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

## Fluid API Structure
Method	                            Description	                            Example Usage
LogEventAsync(message, level)	    Logs a general event	                await logger.LogEventAsync("Something happened", LogLevel.Information);
SendApplicationLogAsync(...)	    Sends structured logs with metadata	    await logger.SendApplicationLogAsync("User logged in", "AuthController.Login", LogLevel.Info);
LogPerformanceAsync(event, time)	Tracks execution time of tasks	        await logger.LogPerformanceAsync("DB Query", 320);


## Advanced Configuration (Optional)
If you need more control, you can pass additional configurations.

var config = new CerbiStreamOptions();
config.SetQueue("Kafka", "kafka://broker-url", "app-logs");
config.DisableDevMode();
config.EnableGovernance();
config.IncludeAdvancedMetadata();

var logger = new CerbiStreamLogger(config);

## Supported Logging Destinations
Queue Type	            Example Usage
RabbitMQ	            QueueType = "RabbitMQ"
Kafka	                QueueType = "Kafka"
Azure Queue	            QueueType = "AzureQueue"
Azure ServiceBus	    QueueType = "AzureServiceBus"
AWS SQS	                QueueType = "AWS_SQS"
AWS Kinesis	            QueueType = "AWS_Kinesis"
Google Pub/Sub	        QueueType = "GooglePubSub"

## Automatic Metadata (No Setup Required)
Metadata Field	        Auto-Detected?	        Example Value
CloudProvider	        Yes	                    AWS, Azure, GCP, On-Prem
Region	                Yes	                    us-east-1, eu-west-2
Environment	            Yes	                    Development, Staging, Production
ApplicationVersion	    Yes	                    v1.2.3
RequestId	            Yes (Generated)	        abc123
TransactionType	        No (Developer Sets)	    REST, gRPC, Kafka
TransactionStatus	    No (Developer Sets)	    Success, Failed

## Governance & Structured Logging
Governance allows organizations to enforce structured logging signatures.
This ensures that logs contain consistent metadata for debugging, security, and AI-driven analytics.

Enforce Required Fields (e.g., every log must include UserId, RequestId, etc.).
Allow Optional Fields (Developers can extend the logs dynamically).
Flexible Governance (Uses cerbi_governance.json for dynamic policy updates).
Example Governance JSON:

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

If GovernanceEnabled = true, logs must match the configured structure.

## Debug Mode (Local Development)

CerbiStream prevents queue logging while debugging. This is enabled by default (EnableDevMode = true).

var config = new CerbiStreamOptions();
config.EnableDevMode();

var logger = new CerbiStreamLogger(config);
await logger.LogEventAsync("Debugging locally", LogLevel.Debug);

## Meta Data Sharing (Opt-In)
CerbiStream collects aggregate trends across applications for AI-powered insights.
No Personally Identifiable Information (PII) is stored.

If enabled, your logs contribute to global analytics (Error Trends, Cloud Performance, API Response Issues).
If disabled, your logs remain 100% private.

var config = new CerbiStreamOptions();
config.IncludeAdvancedMetadata();
config.IncludeSecurityMetadata();

## Why Use CerbiStream?

No External Dependencies – Just install & log.
Optimized Performance – Logs lightweight metadata automatically.
Security First – Encrypts fields, ensures NPI-free logging.
Global Insights – See patterns across multiple industries (if opted-in).
Minimal Setup – Works out-of-the-box with simple constructor injection.

## License
CerbiStream is open-source and available under the MIT License.