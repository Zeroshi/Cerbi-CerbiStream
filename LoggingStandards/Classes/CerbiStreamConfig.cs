using System;

namespace CerbiStream.Configuration
{
    public enum LoggingDestination
    {
        Kafka,
        RabbitMQ,
        AzureServiceBus,
        AWS_SQS,          // ✅ Amazon SQS
        AWS_Kinesis,      // ✅ Amazon Kinesis
        GooglePubSub,     // ✅ Google Pub/Sub
        None              // ✅ Default: No logging
    }

    public class CerbiStreamConfig
    {
        public bool DevModeEnabled { get; set; } = true;  // ✅ Default: True for local debugging
        public LoggingDestination Destination { get; set; } = LoggingDestination.None; // ✅ Default: No logging

        // ✅ Essential Metadata (Minimal, NPI-Free)
        public string CloudProvider { get; set; } = "Unknown";
        public string Region { get; set; } = "Unknown";
        public string InstanceId { get; set; } = "Unknown";
        public string ApplicationVersion { get; set; } = "1.0.0";
    }
}
