using CerbiStream.Enums;
using System.Reflection;

namespace CerbiStream.Configuration
{
    public class CerbiStreamConfig
    {
        // ✅ Cloud & Environment Metadata (Auto-Detected)
        public string CloudProvider { get; private set; }
        public string Region { get; private set; }
        public string Environment { get; private set; }
        public string ApplicationVersion { get; private set; }

        // ✅ Logging Behavior
        public bool EnableDevMode { get; set; } = true;
        public bool EnableEncryption { get; set; } = true;
        public bool IncludeAdvancedMetadata { get; set; } = false;
        public bool IncludeSecurityMetadata { get; set; } = false;
        public bool ApplicationInsightsEnabled { get; set; } = false;

        // ✅ Queue & Destination Settings
        public string QueueType { get; set; } = "RabbitMQ";
        public string QueueConnectionString { get; set; } = "";
        public string QueueName { get; set; } = "default-queue";
        public LoggingDestination Destination { get; set; } = LoggingDestination.None;

        // 🔹 **Constructor for Initializing Auto-Detection**
        public CerbiStreamConfig()
        {
            CloudProvider = DetectCloudProvider();
            Region = DetectRegion();
            Environment = DetectEnvironment();
            ApplicationVersion = GetApplicationVersion();
        }

        // 🔹 **Auto-Detection Methods**
        private string DetectCloudProvider()
        {
            if (System.Environment.GetEnvironmentVariable("AWS_EXECUTION_ENV") != null)
                return "AWS";
            if (System.Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") != null)
                return "GCP";
            if (System.Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null)
                return "Azure";
            return "On-Prem";
        }

        private string DetectRegion()
        {
            return System.Environment.GetEnvironmentVariable("AWS_REGION")
                ?? System.Environment.GetEnvironmentVariable("AZURE_REGION")
                ?? System.Environment.GetEnvironmentVariable("GOOGLE_CLOUD_REGION")
                ?? "Unknown";
        }

        private string DetectEnvironment()
        {
            return System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        }

        private string GetApplicationVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }
    }
}
