# Cerbi Logging Suite

Cerbi is a modern, scalable logging solution designed for distributed systems and cloud environments. It combines logging, message routing, and metadata-driven insights into a unified, developer-friendly platform. Cerbi helps organizations achieve consistent, resource-efficient logging with built-in support for Business Intelligence (BI) and predictive analytics.

---

## 🔑 Key Features

### **CerbiStream (Logging Library)**
- **Developer-Friendly API**: Simple integration with minimal setup.
- **Metadata Capture**: Automatically collects essential metadata (OS, framework, timestamp, cloud provider, region, etc.).
- **Protocol Support**: Compatible with AMQP, HTTPS, REST, and more.
- **Queue Systems**: Works with Azure Service Bus, RabbitMQ, Kafka, and others.

### **CerbiLogIQ (Routing Service)**
- **Dynamic Log Routing**: Distributes logs to appropriate destinations, including:
  - Relational and NoSQL databases
  - Data lakes (Azure Blob Storage, S3, etc.)
  - Log aggregators (Splunk, Loggly)
- **Metadata Parsing**: Enhances routing with intelligent metadata management.

### **CerbiSense (Analytics & Insights)**
- **BI and Reporting**: Integrated with tools like Power BI for visualization.
- **Predictive Analytics**: Enables anomaly detection and performance insights.
- **Customizable Metadata Pools**: Aggregates metadata across systems for better visibility.

---

## 🚀 Get Started

### Installation

The CerbiStream logging library is available on NuGet:
Visit the [NuGet Package](https://www.nuget.org/packages/cerberus-logger-interface/) for details.

### Configuration

Add the following to your `appsettings.json`:
Visit the [NuGet Package](https://www.nuget.org/packages/cerberus-logger-interface/) for details.

### Configuration

Add the following to your `appsettings.json`:
### Usage

#### **Minimal Logging**
#### **Full Logging**
---

## 🏗️ Architecture Overview

CerbiStream provides a seamless logging and routing experience. CerbiLogIQ ensures that logs are routed intelligently, while CerbiSense provides analytics and predictions.

### **Architecture Diagram**
![Cerbi Architecture](Cerbi.png)

1. **CerbiStream**: Captures and encrypts logs programmatically.
2. **CerbiLogIQ**: Manages and routes logs to downstream systems.
3. **CerbiSense**: Aggregates metadata for BI and predictive insights.

---

## 🛠️ Supported Platforms and Protocols

- **Languages**: C#, Java, Python, Go, Node.js, Ruby
- **Queue Systems**: RabbitMQ, Azure Service Bus, Kafka
- **Log Destinations**: SQL/NoSQL databases, Splunk, Loggly, and more
- **Protocols**: AMQP, HTTPS, REST

---

## 🔒 Security and Encryption

Cerbi supports encryption to ensure the security of your logs. You can enable encryption in the configuration:
### Encryption and Decryption

Cerbi uses AES-256 encryption to secure log messages. Here’s how you can encrypt and decrypt messages:

#### **Encryption**
#### **Decryption**
