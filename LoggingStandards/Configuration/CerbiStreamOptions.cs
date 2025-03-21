using CerbiStream.GovernanceAnalyzer;
using CerbiStream.Interfaces;
using System;
using System.Collections.Generic;

namespace CerbiStream.Logging.Configuration
{
    public class CerbiStreamOptions
    {
        public string QueueType { get; private set; } = "RabbitMQ";
        public string QueueHost { get; private set; } = "localhost";
        public string QueueName { get; private set; } = "logs-queue";
        public bool DevModeEnabled { get; private set; } = true;
        public bool AdvancedMetadataEnabled { get; private set; } = false;
        public bool SecurityMetadataEnabled { get; private set; } = false;
        public bool GovernanceEnabled { get; private set; } = false;

        // ✅ Set Queue Configuration
        public void SetQueue(string queueType, string queueHost, string queueName)
        {
            QueueType = queueType;
            QueueHost = queueHost;
            QueueName = queueName;
        }

        //telemetry provider
        public ITelemetryProvider? TelemetryProvider { get; private set; }
        public bool AlsoSendToTelemetry { get; private set; } = false;

        public void SetTelemetryProvider(ITelemetryProvider provider)
        {
            TelemetryProvider = provider;
        }
        public void EnableTelemetryLogging() => AlsoSendToTelemetry = true;


        // ✅ Toggle Dev Mode
        public void EnableDevMode() => DevModeEnabled = true;
        public void DisableDevMode() => DevModeEnabled = false;

        // ✅ Enable Metadata Capture
        public void IncludeAdvancedMetadata() => AdvancedMetadataEnabled = true;
        public void ExcludeAdvancedMetadata() => AdvancedMetadataEnabled = false;
        public void IncludeSecurityMetadata() => SecurityMetadataEnabled = true;
        public void ExcludeSecurityMetadata() => SecurityMetadataEnabled = false;

        // ✅ Validate Logs Against Governance Rules
        public bool ValidateLog(string profileName, Dictionary<string, object> logData)
        {
            if (!GovernanceEnabled) return true;

            return GovernanceAnalyzer.GovernanceAnalyzer.Validate(profileName, logData);
        }
    }
}
