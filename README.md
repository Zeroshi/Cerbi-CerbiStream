# Cerbi Logging Library

A lightweight and flexible structured logging library for capturing application logs with metadata. Designed for use across cloud and on-prem environments, supporting optional encryption and AI/ML-driven insights.

---

## 📌 Features

- **Lightweight** logging with minimal overhead
- **Automatic metadata enrichment** (e.g., CloudProvider, Region)
- **Flexible configuration** via environment variables or manual setup
- **Built-in encryption** (optional, configurable)
- **Support for multiple logging destinations** (e.g., databases, external logging tools)
- **AI/ML compatibility** for trend analysis

---

## 🚀 Installation

### Using NuGet
```sh
dotnet add package Cerbi.Logging

Manually Adding
Clone the repository and add the CerbiClientLogging project to your solution.

🛠️ Setup
Dependency Injection Setup (Recommended)

var serviceProvider = new ServiceCollection()
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });
    })
    .AddSingleton<ITransactionDestination, TransactionDestinationImplementation>()  
    .AddSingleton<ConvertToJson>()
    .AddSingleton<IEncryption, EncryptionImplementation>()
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Logging>>();
var transactionDestination = serviceProvider.GetRequiredService<ITransactionDestination>();
var jsonConverter = serviceProvider.GetRequiredService<ConvertToJson>();
var encryption = serviceProvider.GetRequiredService<IEncryption>();

var logging = new Logging(logger, transactionDestination, jsonConverter, encryption);


📂 Configuration Options
Set configuration options via app settings or environment variables.

Feature	                    Default	        Description
EnableEncryption	        true	        Encrypt all logs unless explicitly disabled
IncludePerformanceMetrics	false	        Track CPU/Memory usage
TransactionDestinationType	Database	    Route logs to a specific destination


Example .appsettings.json:
{
  "LoggingConfig": {
    "EnableEncryption": true,
    "IncludePerformanceMetrics": false,
    "TransactionDestinationType": "Database"
  }
}


------------------

Logging Methods
1️ General Event Logging

await logging.LogEventAsync("User logged in", LogLevel.Information);

2️ Performance Logging

await logging.LogPerformanceAsync("API Request", 250);

3️ Structured Application Logging

await logging.SendApplicationLogAsync(
    applicationMessage: "Order processed",
    currentMethod: "ProcessOrder",
    logLevel: LogLevel.Information,
    log: "Order completed successfully",
    applicationName: "OrderService",
    platform: "Linux",
    onlyInnerException: false,
    note: "Standard processing",
    error: null,
    transactionDestination: transactionDestination,
    transactionDestinationTypes: TransactionDestinationTypes.Other,
    encryption: encryption,
    environment: null,
    identifiableInformation: null,
    payload: "OrderID: 12345",
    cloudProvider: "AWS",
    instanceId: "i-1234567890",
    applicationVersion: "v2.1.0",
    region: "us-east-1",
    requestId: "req-5678"
);

Encryption Handling
How Encryption Works
By default, all logs are encrypted.
Developers can disable encryption globally in the config.
Specific fields can be encrypted selectively.
Decrypting Logs in Downstream Applications
If a downstream application does not use the Cerbi SaaS, it can decrypt logs manually using the same encryption key:

var decryptedLog = encryption.Decrypt(encryptedLog);

License
Cerbi Logging is licensed under the MIT License.

Authors
Thomas Nelson
Cerbi



This README ensures that developers:
- Understand how to **install** and **set up** logging
- Know the **config options**
- Learn **how to log events**
- Can **decrypt logs** manually if needed
- See how **metadata is collected** for AI/ML

Let me know if you need modifications! 
