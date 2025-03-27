using CerbiStream.GovernanceAnalyzer;
using CerbiStream.Interfaces;
using System;
using System.Collections.Generic;
using static CerberusLogging.Classes.Enums.MetaData;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

namespace CerbiStream.Logging.Configuration
{
    public class CerbiStreamOptions
    {
        public string QueueType { get; private set; } = "RabbitMQ";
        public string QueueHost { get; private set; } = "localhost";
        public string QueueName { get; private set; } = "logs-queue";
        public bool AdvancedMetadataEnabled { get; private set; } = false;
        public bool SecurityMetadataEnabled { get; private set; } = false;
        public bool EnableConsoleOutput { get; private set; } = true;
        public bool EnableTelemetryEnrichment { get; private set; } = true;
        public bool EnableMetadataInjection { get; private set; } = true;
        public bool EnableGovernanceChecks { get; private set; } = true;
        public bool DisableQueueSending { get; private set; } = false;

        public ITelemetryProvider? TelemetryProvider { get; private set; }
        public bool AlsoSendToTelemetry { get; private set; } = false;
        public EncryptionType EncryptionMode { get; private set; } = EncryptionType.Base64; // default fallback
        public byte[]? EncryptionKey { get; private set; }
        public byte[]? EncryptionIV { get; private set; }

        public void DumpConfiguration()
        {
            Console.WriteLine("=== CerbiStream Configuration ===");
            Console.WriteLine($"QueueType: {QueueType}");
            Console.WriteLine($"QueueHost: {QueueHost}");
            Console.WriteLine($"QueueName: {QueueName}");
            Console.WriteLine($"Console Output: {EnableConsoleOutput}");
            Console.WriteLine($"Telemetry Enrichment: {EnableTelemetryEnrichment}");
            Console.WriteLine($"Metadata Injection: {EnableMetadataInjection}");
            Console.WriteLine($"Governance Checks: {EnableGovernanceChecks}");
            Console.WriteLine($"Queue Disabled: {DisableQueueSending}");
            Console.WriteLine($"Telemetry Logging: {AlsoSendToTelemetry}");
            Console.WriteLine($"Encryption Mode: {EncryptionMode}");
            Console.WriteLine($"Encryption Enabled: {(EncryptionKey != null && EncryptionIV != null)}");
            Console.WriteLine("=================================");
        }


        public CerbiStreamOptions WithEncryptionKey(byte[] key, byte[] iv)
        {
            EncryptionKey = key;
            EncryptionIV = iv;
            return this;
        }

        public CerbiStreamOptions WithEncryptionMode(EncryptionType type)
        {
            EncryptionMode = type;
            return this;
        }

        public CerbiStreamOptions WithQueue(string type, string host, string name)
        {
            QueueType = type;
            QueueHost = host;
            QueueName = name;
            return this;
        }

        public CerbiStreamOptions WithTelemetryProvider(ITelemetryProvider provider)
        {
            TelemetryProvider = provider;
            return this;
        }

        public CerbiStreamOptions WithAdvancedMetadata(bool enabled = true)
        {
            AdvancedMetadataEnabled = enabled;
            return this;
        }

        public CerbiStreamOptions WithSecurityMetadata(bool enabled = true)
        {
            SecurityMetadataEnabled = enabled;
            return this;
        }

        public CerbiStreamOptions WithConsoleOutput(bool enabled = true)
        {
            EnableConsoleOutput = enabled;
            return this;
        }

        public CerbiStreamOptions WithTelemetryEnrichment(bool enabled = true)
        {
            EnableTelemetryEnrichment = enabled;
            return this;
        }

        public CerbiStreamOptions WithMetadataInjection(bool enabled = true)
        {
            EnableMetadataInjection = enabled;
            return this;
        }

        public CerbiStreamOptions WithTelemetryLogging(bool enabled = true)
        {
            AlsoSendToTelemetry = enabled;
            return this;
        }

        public CerbiStreamOptions WithGovernanceChecks(bool enabled = true)
        {
            EnableGovernanceChecks = enabled;
            return this;
        }

        public CerbiStreamOptions WithDisableQueue(bool disabled = true)
        {
            DisableQueueSending = disabled;
            return this;
        }

        public CerbiStreamOptions EnableBenchmarkMode()
        {
            return WithConsoleOutput(false)
                .WithTelemetryEnrichment(false)
                .WithMetadataInjection(false)
                .WithGovernanceChecks(false)
                .WithDisableQueue(true);
        }

        public CerbiStreamOptions EnableDeveloperModeWithTelemetry()
        {
            return WithTelemetryLogging(true)
                .WithConsoleOutput(true)
                .WithTelemetryEnrichment(true)
                .WithMetadataInjection(true)
                .WithGovernanceChecks(false);
        }

        public CerbiStreamOptions EnableDeveloperModeWithoutTelemetry()
        {
            return WithTelemetryLogging(false)
                .WithConsoleOutput(true)
                .WithTelemetryEnrichment(false)
                .WithMetadataInjection(true)
                .WithGovernanceChecks(false);
        }

        public CerbiStreamOptions EnableDevModeMinimal()
        {
            return WithTelemetryLogging(false)
                .WithConsoleOutput(true)
                .WithTelemetryEnrichment(false)
                .WithMetadataInjection(false)
                .WithGovernanceChecks(false);
        }

        public bool ValidateLog(string profileName, Dictionary<string, object> logData)
        {
            if (!EnableGovernanceChecks) return true;
            return GovernanceAnalyzer.GovernanceAnalyzer.Validate(profileName, logData);
        }

        public bool ShouldSkipQueueSend() => DisableQueueSending;

        public bool IsBenchmarkMode =>
            !EnableConsoleOutput &&
            !EnableTelemetryEnrichment &&
            !EnableMetadataInjection &&
            !EnableGovernanceChecks &&
            DisableQueueSending;

        public bool IsMinimalMode =>
            EnableConsoleOutput &&
            !EnableTelemetryEnrichment &&
            !EnableMetadataInjection &&
            !EnableGovernanceChecks;

        public bool IsDevWithTelemetry =>
            EnableConsoleOutput &&
            EnableTelemetryEnrichment &&
            EnableMetadataInjection &&
            !EnableGovernanceChecks;

        public bool IsDevWithoutTelemetry =>
            EnableConsoleOutput &&
            !EnableTelemetryEnrichment &&
            EnableMetadataInjection &&
            !EnableGovernanceChecks;
    }
}
