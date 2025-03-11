# CerbiStream Logging Library
CerbiStream provides a seamless, low-config logging solution that integrates directly into your app with minimal setup. It supports structured logs, queue-based log routing, and optional metadata sharing to improve observability across cloud and on-prem environments.

## What's New?
- **Fluid API Setup** – No complex configurations, just pass details during initialization.
- **Plug-and-Play Cloud Detection** – Auto-detects environment (AWS, Azure, GCP, On-Prem).
- **No External Dependencies Required** – Handles queue setup internally.
- **Dev Mode** – Prevents logs from being sent to external queues while debugging.
- **Secure & NPI-Free Data Collection** – Captures useful metadata without storing sensitive user data.

## Installation

## Quick Start (Minimal Setup)
With the new Fluid API, you only need to inject CerbiStream into your app.
```csharp
using CerbiStream;
using CerbiStream.Configuration;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        var logging = new CerbiStreamLogger(new CerbiStreamConfig
        {
            QueueType = "RabbitMQ",
            QueueConnectionString = "your-queue-connection",
            QueueName = "app-logs",
            EnableDevMode = true 
        });

        await logging.LogEventAsync("Application started", LogLevel.Information);
        Console.WriteLine("Log sent successfully!");
    }
}

## Fluid API Structure

Method                                           Description	                                                     Example
LogEventAsync(message, level)	                 Logs a general event	                                             await logger.LogEventAsync("Something happened", LogLevel.Information);
SendApplicationLogAsync(...)	                 Sends structured logs with metadata	                             await logger.SendApplicationLogAsync("User logged in", "AuthController.Login", LogLevel.Info);
LogPerformanceAsync(event, timeMs)	             Tracks execution time of tasks	                                     await logger.LogPerformanceAsync("DB Query", 320);

## Advanced Configuration (Optional)

If you need more control, you can still pass more configurations.

var config = new CerbiStreamConfig
{
    QueueType = "Kafka",
    QueueConnectionString = "kafka://broker-url",
    QueueName = "app-logs",
    EnableDevMode = false,
    EnableEncryption = true,
    IncludeAdvancedMetadata = true,
    IncludeSecurityMetadata = false
};

var logger = new CerbiStreamLogger(config);

## Supported Logging Destinations

Queue Type	            Example Usage
RabbitMQ	            QueueType = "RabbitMQ"
Kafka	                QueueType = "Kafka"
Azure Queue Storage 	QueueType = "AzureQueue"
Azure Service Bus	    QueueType = "AzureServiceBus"
AWS SQS	                QueueType = "AWS_SQS"
AWS Kinesis	            QueueType = "AWS_Kinesis"
Google Pub/Sub	        QueueType = "GooglePubSub"

## Automatic Metadata (No Setup Required)

Metadata Field	        Auto-Detected?	        Example Value
CloudProvider	        ✅ Yes	                AWS, Azure, GCP, On-Prem
Region	                ✅ Yes	                us-east-1, eu-west-2
Environment	            ✅ Yes	                Development, Staging, Production
ApplicationVersion	    ✅ Yes	                v1.2.3
RequestId	            ✅ Yes (Generated)	    abc123
TransactionType	        ❌ Developer Sets	    REST, gRPC, Kafka
TransactionStatus	    ❌ Developer Sets	    Success, Failed

## Debug Mode (Local Development)
CerbiStream prevents queue logging while debugging. This is enabled by default (EnableDevMode = true).


var config = new CerbiStreamConfig { EnableDevMode = true };
var logger = new CerbiStreamLogger(config);

await logger.LogEventAsync("Debugging locally", LogLevel.Debug);

## Meta Data Sharing (Opt-In)

CerbiStream collects aggregate trends across applications for AI-powered insights.
No Personally Identifiable Information (PII) is stored.

If enabled, your logs contribute to global analytics (Error Trends, Cloud Performance, API Response Issues).
If disabled, your logs remain 100% private.

var config = new CerbiStreamConfig
{
    EnableDevMode = false,
    IncludeAdvancedMetadata = true,
    IncludeSecurityMetadata = false 
};

## Why Use CerbiStream?

No External Dependencies – Just install & log.
Optimized Performance – Logs lightweight metadata automatically.
Security First – Encrypts fields, ensures NPI-free logging.
Global Insights – See patterns across multiple industries (if opted-in).
Minimal Setup – Works out-of-the-box with simple constructor injection.

## License

CerbiStream is open-source and available under the MIT License.