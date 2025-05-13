using CerbiStream.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;
using FileFallbackOptions = CerbiStream.Classes.FileLogging.FileFallbackOptions;

namespace CerbiStream.Logging.Configuration
{
    public class CerbiStreamOptions
    {
        /// <summary>
        /// Configuration settings for local file fallback logging.
        /// Used if primary queue or remote sinks fail.
        /// </summary>
        public FileFallbackOptions? FileFallback { get; set; }

        /// <summary>
        /// Defines the hosting model of the application (e.g., API, WebApp, Worker, Function).
        /// Helps classify logs for environment and scaling characteristics.
        /// </summary>
        public string? ApplicationType { get; private set; }

        /// <summary>
        /// Defines the business domain role of the service (e.g., IdentityService, PaymentService, OrderService).
        /// Useful for tracking log trends by business purpose.
        /// </summary>
        public string? ServiceType { get; private set; }

        /// <summary>
        /// Defines the target application type this service interacts with (optional).
        /// Helps when tracing cross-service communication.
        /// </summary>
        public string? TargetApplicationType { get; private set; }

        /// <summary>
        /// Defines the target service type this service interacts with (optional).
        /// Useful for dependency tracing.
        /// </summary>
        public string? TargetServiceType { get; private set; }

        /// <summary>
        /// Enables automatic enrichment of tracing metadata like TraceId, SpanId, ParentSpanId if available.
        /// Useful for lightweight distributed tracing.
        /// </summary>
        public bool EnableTracingEnrichment { get; private set; } = false;

        /// <summary>
        /// Type of queue backend to use (e.g., RabbitMQ, AzureQueue, Kafka).
        /// </summary>
        public string QueueType { get; private set; } = "RabbitMQ";

        /// <summary>
        /// Hostname or endpoint for the queue backend.
        /// </summary>
        public string QueueHost { get; private set; } = "localhost";

        /// <summary>
        /// Queue name to send logs to.
        /// </summary>
        public string QueueName { get; private set; } = "logs-queue";

        /// <summary>
        /// Enables additional environment metadata like CloudProvider, Region.
        /// </summary>
        public bool AdvancedMetadataEnabled { get; private set; } = false;

        /// <summary>
        /// Enables security-related metadata enrichment, e.g., masking user IDs.
        /// </summary>
        public bool SecurityMetadataEnabled { get; private set; } = false;

        /// <summary>
        /// Enables log output to local console for dev/debug scenarios.
        /// </summary>
        public bool EnableConsoleOutput { get; private set; } = true;

        /// <summary>
        /// Enables telemetry data enrichment if configured (e.g., OpenTelemetry).
        /// </summary>
        public bool EnableTelemetryEnrichment { get; private set; } = true;

        /// <summary>
        /// Enables metadata auto-injection into every log event.
        /// </summary>
        public bool EnableMetadataInjection { get; private set; } = true;

        /// <summary>
        /// Enables governance validation for structured log compliance.
        /// </summary>
        public bool EnableGovernanceChecks { get; private set; } = true;

        /// <summary>
        /// If true, logs will not be sent to a queue.
        /// Useful for benchmark or minimal setups.
        /// </summary>
        public bool DisableQueueSending { get; private set; } = false;

        /// <summary>
        /// Optional telemetry provider integration (OpenTelemetry, AppInsights).
        /// </summary>
        public ITelemetryProvider? TelemetryProvider { get; private set; }

        /// <summary>
        /// If true, also sends logs to telemetry provider if configured.
        /// </summary>
        public bool AlsoSendToTelemetry { get; private set; } = false;

        /// <summary>
        /// Payload encryption mode (None, Base64, AES).
        /// </summary>
        public EncryptionType EncryptionMode { get; private set; } = EncryptionType.None;

        /// <summary>
        /// Symmetric encryption key if AES encryption is enabled.
        /// </summary>
        public byte[]? EncryptionKey { get; private set; }

        /// <summary>
        /// Symmetric encryption IV (Initialization Vector) for AES.
        /// </summary>
        public byte[]? EncryptionIV { get; private set; }

        /// <summary>
        /// Enable Polly retry logic when queue send fails.
        /// </summary>
        public bool EnableQueueRetries { get; private set; } = true;

        /// <summary>
        /// Number of retry attempts if queue send fails.
        /// </summary>
        public int QueueRetryCount { get; private set; } = 3;

        /// <summary>
        /// Delay between retry attempts (in milliseconds).
        /// </summary>
        public int QueueRetryDelayMilliseconds { get; private set; } = 200;

        // Additional configuration flags for operational modes
        public bool MinimalMode { get; private set; } = false;
        public bool FullMode { get; private set; } = false;

        /// <summary>
        /// Custom governance validation hook to enforce log compliance.
        /// </summary>
        public Func<string, Dictionary<string, object>, bool>? GovernanceValidator { get; private set; }


        /// <summary>
        /// Switches the configuration to Minimal Mode.
        /// Skips tracing enrichment for performance-focused environments.
        /// </summary>
        public CerbiStreamOptions EnableMinimalMode()
        {
            MinimalMode = true;
            FullMode = false;
            EnableTracingEnrichment = false;
            return this;
        }

        /// <summary>
        /// Switches the configuration to Full Mode.
        /// Enables full tracing enrichment for comprehensive observability.
        /// </summary>
        public CerbiStreamOptions EnableFullMode()
        {
            MinimalMode = false;
            FullMode = true;
            EnableTracingEnrichment = true;
            return this;
        }

        /// <summary>
        /// Sets the application hosting model and business service role for log metadata enrichment.
        /// </summary>
        public CerbiStreamOptions WithApplicationIdentity(string applicationType, string serviceType)
        {
            ApplicationType = applicationType;
            ServiceType = serviceType;
            return this;
        }

        /// <summary>
        /// Sets target system details for enhanced cross-system tracing.
        /// </summary>
        public CerbiStreamOptions WithTargetSystem(string targetApplicationType, string targetServiceType)
        {
            TargetApplicationType = targetApplicationType;
            TargetServiceType = targetServiceType;
            return this;
        }

        /// <summary>
        /// Enables or disables automatic tracing enrichment into log metadata.
        /// </summary>
        public CerbiStreamOptions WithTracingEnrichment(bool enabled = true)
        {
            EnableTracingEnrichment = enabled;
            return this;
        }

        /// <summary>
        /// Logical service name used to tag logs (e.g., OrderService, AuthService).
        /// Helps distinguish different services inside the same app environment.
        /// </summary>
        public string? ServiceName { get; private set; }

        /// <summary>
        /// Name of the root application or client that triggered the logging.
        /// Helps with distributed tracing and identifying origin points.
        /// </summary>
        public string? OriginApp { get; private set; }

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

        public CerbiStreamOptions WithoutEncryption() => WithEncryptionMode(EncryptionType.None);
        public CerbiStreamOptions WithBase64Encryption() => WithEncryptionMode(EncryptionType.Base64);
        public CerbiStreamOptions WithAesEncryption() => WithEncryptionMode(EncryptionType.AES);

        public CerbiStreamOptions WithQueue(string type, string host, string name)
        {
            QueueType = type;
            QueueHost = host;
            QueueName = name;
            return this;
        }

        public CerbiStreamOptions WithQueueRetries(bool enabled, int retryCount = 3, int delayMilliseconds = 200)
        {
            EnableQueueRetries = enabled;
            QueueRetryCount = retryCount;
            QueueRetryDelayMilliseconds = delayMilliseconds;
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

        public CerbiStreamOptions WithGovernanceValidator(Func<string, Dictionary<string, object>, bool> validator)
        {
            GovernanceValidator = validator;
            return this;
        }

        public CerbiStreamOptions WithFileFallback(CerbiStream.Classes.FileLogging.FileFallbackOptions options)
        {
            FileFallback = options;
            return this;
        }



        public CerbiStreamOptions EnableProductionMode()
        {
            return WithTelemetryLogging(true)
                .WithConsoleOutput(false)
                .WithTelemetryEnrichment(true)
                .WithMetadataInjection(true)
                .WithGovernanceChecks(true)
                .WithDisableQueue(false);
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

        public bool ValidateLog(string profileName, Dictionary<string, object> logData) =>
            !EnableGovernanceChecks || (GovernanceValidator?.Invoke(profileName, logData) ?? true);

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
