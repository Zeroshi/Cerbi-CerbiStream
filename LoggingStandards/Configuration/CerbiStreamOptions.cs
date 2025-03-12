using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json; // Ensure Newtonsoft.Json is referenced

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
                Console.WriteLine("🔹 No governance file found. Proceeding without governance enforcement.");
                GovernanceEnabled = false;
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                GovernanceData = JsonConvert.DeserializeObject<CerbiGovernance>(json);
                Console.WriteLine("✅ Governance JSON Loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading governance: {ex.Message}. Governance disabled.");
                GovernanceEnabled = false;
            }
        }

        // ✅ Validate Logs Against Governance Rules
        public bool ValidateLog(string profileName, Dictionary<string, object> logData)
        {
            if (!GovernanceEnabled || GovernanceData == null || !GovernanceData.LoggingProfiles.ContainsKey(profileName))
                return true;

            var profile = GovernanceData.LoggingProfiles[profileName];
            foreach (var requiredField in profile.RequiredFields)
            {
                if (!logData.ContainsKey(requiredField))
                {
                    Console.WriteLine($"❌ Missing required field: {requiredField}");
                    return false;
                }
            }

            return true;
        }
    }
}
