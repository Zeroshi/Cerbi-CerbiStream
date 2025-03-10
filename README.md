# Cerbi Logging Library

## Overview
Cerbi Logging Library is a lightweight, extensible logging system with support for multiple queue integrations (RabbitMQ, Azure Service Bus, Kafka, AWS SQS, Google Pub/Sub, etc.). It now supports **Fluent API-based configuration**, replacing the old config-driven approach.

---

## New Features in This Update
- **Fluent API for Queue Configuration**
  - No more JSON config files. Configure queues directly in the code.
- **Improved Dependency Injection**
  - Inject `IQueue` dynamically using dependency injection.
- **Enhanced Unit Tests**
  - Improved test coverage for logging behavior, queue selection, and encryption.
- **Updated Encryption Handling**
  - Sensitive metadata fields (`APIKey`, `SensitiveField`) are now automatically encrypted if enabled.

---

## Installation

dotnet add package CerbiClientLogging
💡 Usage Example (New Fluent API)
csharp
Copy
Edit
using CerbiClientLogging.Classes;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceProvider = new ServiceCollection()
    .AddLogging(logging => logging.AddConsole())
    .AddSingleton<IQueue, RabbitMqQueue>(provider => new RabbitMqQueue("rabbitmq-host", "queue-name"))
    .AddSingleton<IConvertToJson, ConvertToJson>()
    .AddSingleton<IEncryption, EncryptionImplementation>()
    .AddSingleton<IBaseLogging, Logging>()
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<IBaseLogging>();
await logger.LogEventAsync("Hello, Fluent Logging!", LogLevel.Information);

Fluent API Configuration
Step 1: Choose a Queue
csharp
Copy
Edit
var logger = new CerbiLoggerBuilder()
    .UseRabbitMQ("rabbitmq-connection-string")
    .EnableEncryption(true)
    .Build(serviceProvider.GetRequiredService<ILogger<Logging>>(), 
           new ConvertToJson(), 
           new EncryptionImplementation());

Queue Type	            Fluent API
RabbitMQ	            .UseRabbitMQ("connectionString")
Azure Service Bus	    .UseAzureServiceBus("connectionString", "queueName")
Kafka	                .UseKafka("brokerList", "topic")
AWS SQS	                .UseSqs("accessKey", "secretKey", "region", "queueUrl")
