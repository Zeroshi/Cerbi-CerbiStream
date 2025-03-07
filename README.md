# CerbiClientLogging Library

## Overview

The **CerbiClientLogging** library is designed to provide structured logging with rich metadata collection for cloud-based and on-prem applications. It enables developers to capture logging data efficiently while ensuring security, flexibility, and ease of integration with various logging destinations.

This library supports **optional encryption**, ensuring that logs can be securely transmitted and stored. The metadata collected can be used for **machine learning (ML) and artificial intelligence (AI) analysis**, helping identify patterns, trends, and system performance insights.

---

## Features

- **Structured Logging**: Logs are formatted in JSON for easy parsing.
- **Flexible Encryption**: Supports full or per-field encryption for security.
- **Metadata Collection**: Automatically captures cloud provider, instance ID, application version, and more.
- **Performance Logging**: Tracks event durations and system resource usage.
- **Configurable Storage**: Logs can be sent to databases, message queues, APIs, or other destinations.
- **Cross-Platform**: Supports .NET applications, with future implementations planned for Java and Python.

---

## Installation

To install the package, add it to your project using:

##dotnet add package CerbiClientLogging


Ensure that your application includes the following dependencies:
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>

Configuration
AppSettings.json Example
The logging behavior is configured through appsettings.json:

{
  "Logging": {
    "EnableEncryption": true,
    "Destination": "Database",
    "IncludePerformanceMetrics": false
  },
  "Encryption": {
    "Key": "your-encryption-key-here"
  }
}

Environment Variables (Alternative to JSON Config)
export LOGGING_ENABLE_ENCRYPTION=true
export LOGGING_DESTINATION=Database
export ENCRYPTION_KEY="your-encryption-key-here"

##Implementation
###Step 1: Configure Dependency Injection
In Program.cs (for ASP.NET Core applications):

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
    .AddSingleton<IEncryption, EncryptionImplementation>()
    .AddSingleton<ConvertToJson>()
    .AddSingleton<Logging>()
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Logging>>();
var transactionDestination = serviceProvider.GetRequiredService<ITransactionDestination>();
var jsonConverter = serviceProvider.GetRequiredService<ConvertToJson>();
var encryption = serviceProvider.GetRequiredService<IEncryption>();

var logging = new Logging(logger, transactionDestination, jsonConverter, encryption);


###Step 2: Logging Events
Basic Event Logging

await logging.LogEventAsync("User logged in", LogLevel.Information);

Performance Logging
await logging.LogPerformanceAsync("API Request", 123); // 123 ms execution time

Detailed Application Logging
await logging.SendApplicationLogAsync(
    applicationMessage: "User authentication successful",
    currentMethod: "Login",
    logLevel: LogLevel.Information,
    log: "Authentication passed",
    applicationName: "MyApp",
    platform: "Windows",
    onlyInnerException: false,
    note: "User authenticated successfully",
    error: null,
    transactionDestination: transactionDestination,
    transactionDestinationTypes: TransactionDestinationTypes.Database,
    encryption: encryption,
    environment: null,
    identifiableInformation: null,
    payload: "SessionID: abc123",
    cloudProvider: "AWS",
    instanceId: "i-123456",
    applicationVersion: "1.2.3",
    region: "us-east-1",
    requestId: Guid.NewGuid().ToString()
);

Data Flow & Metadata Collection
| Field Name         | Description                               | Example Value |
|--------------------|-------------------------------------------|--------------|
| CloudProvider     | AWS, Azure, GCP, On-Prem                 | AWS          |
| InstanceId        | Machine or VM Instance ID                | i-123456     |
| ApplicationVersion| The version of the running application   | 1.2.3        |
| Region           | Cloud region of deployment               | us-east-1    |
| RequestId        | Unique request ID for tracking logs      | abc123       |
| CPUUsage         | Captured if enabled in settings          | 55%          |
| MemoryUsage      | Captured if enabled in settings          | 1.5GB        |


Encryption Handling
By default, encryption is enabled.

To disable encryption:
var logging = new Logging(logger, transactionDestination, jsonConverter, encryption, enableEncryption: false);


Decrypting Logs in Downstream Applications
If another application needs to decrypt logs, use the IEncryption interface:

string decryptedLog = encryption.Decrypt(encryptedLog);
Console.WriteLine($"Decrypted Log: {decryptedLog}");

If the SaaS product is used, decryption happens automatically within the system.

Optional Features

| Feature                      | Default Setting | Purpose |
|------------------------------|----------------|---------|
| EnableEncryption             | true           | Encrypts all logs unless turned off |
| IncludePerformanceMetrics    | false          | Tracks CPU/Memory Usage |
| TransactionDestinationType   | Database       | Allows logs to be routed to different storage locations |



Example Output (JSON Format)
A fully formatted log entry looks like this:

{
  "TimestampUtc": "2025-03-05T14:30:00Z",
  "LogLevel": "Information",
  "Message": "User authentication successful",
  "Metadata": {
    "CloudProvider": "AWS",
    "InstanceId": "i-123456",
    "ApplicationVersion": "1.2.3",
    "Region": "us-east-1",
    "RequestId": "abc123",
    "Log": "Authentication passed",
    "Platform": "Windows",
    "OnlyInnerException": false,
    "Note": "User authenticated successfully"
  }
}

License
This project is licensed under the MIT License.

