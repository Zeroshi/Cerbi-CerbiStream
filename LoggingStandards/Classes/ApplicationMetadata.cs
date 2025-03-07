using System;

namespace CerbiClientLogging.Classes
{
    public static class ApplicationMetadata
    {
        public static readonly string CloudProvider;
        public static readonly string Region;
        public static readonly string InstanceId;
        public static readonly string ApplicationId = "MyApp";  // Can be overridden in config
        public static readonly string ApplicationVersion = "1.2.3";  // Pulled from Assembly Info

        static ApplicationMetadata()
        {
            CloudProvider = DetectCloudProvider();
            Region = DetectRegion();
            InstanceId = DetectInstanceId();
        }

        private static string DetectCloudProvider()
        {
            if (Environment.GetEnvironmentVariable("AWS_EXECUTION_ENV") != null) return "AWS";
            if (Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") != null) return "GCP";
            if (Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null) return "Azure";
            return "On-Prem";
        }

        private static string DetectRegion()
        {
            return Environment.GetEnvironmentVariable("CLOUD_REGION") ?? "unknown-region";
        }

        private static string DetectInstanceId()
        {
            return Environment.MachineName;
        }
    }
}
