using System;

namespace CerbiStream.Classes
{
    public static class ApplicationMetadata
    {
        public static string CloudProvider
        {
            get
            {
                if (Environment.GetEnvironmentVariable("AWS_EXECUTION_ENV") != null) return "AWS";
                if (Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") != null) return "GCP";
                if (Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null) return "Azure";
                return "On-Prem";
            }
        }

        public static string Region => Environment.GetEnvironmentVariable("CLOUD_REGION") ?? "unknown-region";

        public static string InstanceId => Environment.MachineName;

        public static string ApplicationId => "MyApp";  // Can be overridden in config

        public static string ApplicationVersion => "1.2.3";  // Pulled from Assembly Info
    }
}
