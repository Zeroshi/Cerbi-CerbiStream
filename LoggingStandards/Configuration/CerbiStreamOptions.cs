using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json; // Ensure Newtonsoft.Json is referenced
using CerbiStream.GovernanceAnalyzer;

namespace CerbiStream.Logging.Configuration
{
    public class CerbiGovernance
    {
        public Dictionary<string, LogProfile> LoggingProfiles { get; set; } = new();
    }

    public class LogProfile
    {
        public List<string> RequiredFields { get; set; } = new();
        public List<string> OptionalFields { get; set; } = new();
    }

    public class CerbiStreamOptions
    {
        public string QueueType { get; private set; } = "RabbitMQ";
        public string QueueHost { get; private set; } = "localhost";
        public string QueueName { get; private set; } = "logs-queue";
        public bool DevModeEnabled { get; private set; } = true;
        public bool AdvancedMetadataEnabled { get; private set; } = false;
        public bool SecurityMetadataEnabled { get; private set; } = false;
        public bool GovernanceEnabled { get; private set; } = false;

        private CerbiGovernance? GovernanceData;

        // ✅ Set Queue Configuration
        public void SetQueue(string queueType, string queueHost, string queueName)
        {
            QueueType = queueType;
            QueueHost = queueHost;
            QueueName = queueName;
        }

        // ✅ Toggle Dev Mode
        public void EnableDevMode() => DevModeEnabled = true;
        public void DisableDevMode() => DevModeEnabled = false;

        // ✅ Enable Metadata Capture
        public void IncludeAdvancedMetadata() => AdvancedMetadataEnabled = true;
        public void ExcludeAdvancedMetadata() => AdvancedMetadataEnabled = false;
        public void IncludeSecurityMetadata() => SecurityMetadataEnabled = true;
        public void ExcludeSecurityMetadata() => SecurityMetadataEnabled = false;

        // ✅ Enable Governance (Optional JSON File)
        public void EnableGovernance()
        {
            GovernanceEnabled = true;
            LoadGovernance();
        }

        private void LoadGovernance()
        {
            string filePath = "cerbi_governance.json";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("🔹 No governance file found. Proceeding with default governance.");
                return; // ✅ Governance stays enabled even if file is missing
            }

            try
            {
                string json = File.ReadAllText(filePath);
                GovernanceData = JsonConvert.DeserializeObject<CerbiGovernance>(json);
                Console.WriteLine("✅ Governance JSON Loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading governance: {ex.Message}. Proceeding with default governance.");
            }
        }

        // ✅ Validate Logs Against Governance Rules
        public bool ValidateLog(string profileName, Dictionary<string, object> logData)
        {
            if (!GovernanceEnabled) return true;

            return GovernanceAnalyzer.GovernanceAnalyzer.Validate(profileName, logData);
        }

    }
}
