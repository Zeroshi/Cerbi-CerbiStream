using CerbiStream.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;
using FileFallbackOptions = CerbiStream.Classes.FileLogging.FileFallbackOptions;

namespace CerbiStream.Logging.Configuration
{
    /// <summary>
    /// Configuration options for CerbiStream logging.
    /// Use fluent methods to configure, or use preset methods for common scenarios.
    /// </summary>
    /// <example>
    /// Quick setup for development:
    /// <code>
    /// builder.Logging.AddCerbiStream(); // Uses EnableDeveloperMode() by default
    /// </code>
    /// 
    /// Production setup:
    /// <code>
    /// builder.Logging.AddCerbiStream(o => o.ForProduction());
    /// </code>
    /// 
    /// Custom configuration:
    /// <code>
    /// builder.Logging.AddCerbiStream(o => o
    ///     .WithGovernanceChecks(true)
    ///     .WithQueueRetries(true, 5, 500)
    ///     .WithAesEncryption());
    /// </code>
    /// </example>
    public class CerbiStreamOptions
    {
        /// <summary>
        /// Configuration settings for local file fallback logging.
        /// </summary>
        public CerbiStream.Classes.FileLogging.FileFallbackOptions? FileFallback { get; set; }

        /// <summary>
        /// Enables async console output using background queue.
        /// </summary>
        public bool EnableAsyncConsoleOutput { get; private set; } = false;

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
        public bool EnableGovernanceChecks { get; private set; } = false;

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

        // NEW: Governance wiring options for AddCerbiStream governance mode
        public string GovernanceProfileName { get; private set; } = "default";
        public string? GovernanceConfigPath { get; private set; }
        public Func<ILoggerFactory>? InnerFactoryProvider { get; set; }

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

        public CerbiStreamOptions WithAsyncConsoleOutput(bool enabled = true)
        {
            EnableAsyncConsoleOutput = enabled;
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
            EnableGovernanceChecks = true;
            return this;
        }

        // NEW: Fluent config for governance path
        public CerbiStreamOptions WithGovernanceProfile(string profileName)
        {
            GovernanceProfileName = string.IsNullOrWhiteSpace(profileName) ? "default" : profileName;
            return this;
        }

        public CerbiStreamOptions WithGovernanceConfigPath(string? path)
        {
            GovernanceConfigPath = path;
            return this;
        }

        public CerbiStreamOptions WithInnerFactoryProvider(Func<ILoggerFactory> provider)
        {
            InnerFactoryProvider = provider;
            return this;
        }

        /// <summary>
        /// Enables local file fallback logging with the provided options.
        /// </summary>
        public CerbiStreamOptions WithFileFallback(CerbiStream.Classes.FileLogging.FileFallbackOptions options)
        {
            FileFallback = options;
            return this;
        }

        /// <summary>
        /// Enables file fallback logging using default fallback path.
        /// </summary>
        public CerbiStreamOptions WithFileFallback()
        {
            FileFallback = new FileFallbackOptions
            {
                Enable = true,
                FallbackFilePath = "logs/log-fallback.json",
                PrimaryFilePath = "logs/log-primary.json"
            };
            ValidateFallbackOptions();
            return this;
        }

        /// <summary>
        /// Enables file fallback logging with a custom fallback file path.
        /// </summary>
        public CerbiStreamOptions WithFileFallback(string fallbackFilePath)
        {
            FileFallback = new FileFallbackOptions
            {
                Enable = true,
                FallbackFilePath = fallbackFilePath,
                PrimaryFilePath = "logs/log-primary.json"
            };
            ValidateFallbackOptions();
            return this;
        }

        /// <summary>
        /// Enables file fallback logging with both custom fallback and primary paths.
        /// </summary>
        public CerbiStreamOptions WithFileFallback(string fallbackFilePath, string primaryFilePath)
        {
            FileFallback = new FileFallbackOptions
            {
                Enable = true,
                FallbackFilePath = fallbackFilePath,
                PrimaryFilePath = primaryFilePath
            };
            ValidateFallbackOptions();
            return this;
        }

        /// <summary>
        /// Enables file fallback logging with encryption options.
        /// </summary>
        public CerbiStreamOptions WithEncryptedFallback(string fallbackFilePath, string primaryFilePath, string encryptionKey, string encryptionIV)
        {
            FileFallback = new FileFallbackOptions
            {
                Enable = true,
                FallbackFilePath = fallbackFilePath,
                PrimaryFilePath = primaryFilePath,
                EncryptionKey = encryptionKey,
                EncryptionIV = encryptionIV
            };
            ValidateFallbackOptions();
            return this;
        }

        /// <summary>
        /// Enables file fallback logging with encryption and file limits.
        /// </summary>
        public CerbiStreamOptions WithEncryptedFallback(string fallbackFilePath, string primaryFilePath, string encryptionKey, string encryptionIV, long maxFileSizeBytes, TimeSpan maxFileAge)
        {
            FileFallback = new FileFallbackOptions
            {
                Enable = true,
                FallbackFilePath = fallbackFilePath,
                PrimaryFilePath = primaryFilePath,
                EncryptionKey = encryptionKey,
                EncryptionIV = encryptionIV,
                MaxFileSizeBytes = maxFileSizeBytes,
                MaxFileAge = maxFileAge
            };
            ValidateFallbackOptions();
            return this;
        }

        /// <summary>
        /// Validates fallback options to prevent misconfiguration.
        /// </summary>
        private void ValidateFallbackOptions()
        {
            if (FileFallback == null)
                throw new InvalidOperationException("FileFallback options must be configured.");

            if (string.IsNullOrWhiteSpace(FileFallback.FallbackFilePath))
                throw new ArgumentException("FallbackFilePath must be provided.");

            if (string.IsNullOrWhiteSpace(FileFallback.PrimaryFilePath))
                throw new ArgumentException("PrimaryFilePath must be provided.");

            if (FileFallback.MaxFileSizeBytes < 1024)
                throw new ArgumentException("MaxFileSizeBytes must be at least 1KB.");

            if (!string.IsNullOrEmpty(FileFallback.EncryptionKey) ^ !string.IsNullOrEmpty(FileFallback.EncryptionIV))
                throw new ArgumentException("Both EncryptionKey and EncryptionIV must be provided together.");
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

        // ============================================
        // DEVELOPER-FIRST PRESET METHODS
        // ============================================

        /// <summary>
        /// 🚀 Quick setup for development. Console output enabled, governance auto-configured.
        /// This is the default when calling AddCerbiStream() with no parameters.
        /// </summary>
        /// <example>
        /// <code>
        /// builder.Logging.AddCerbiStream(); // Uses this preset automatically
        /// // or explicitly:
        /// builder.Logging.AddCerbiStream(o => o.EnableDeveloperMode());
        /// </code>
        /// </example>
        public CerbiStreamOptions EnableDeveloperMode()
        {
            return WithConsoleOutput(true)
                .WithTelemetryEnrichment(false)
                .WithMetadataInjection(true)
                .WithGovernanceChecks(true)  // PII protection even in dev
                .WithDisableQueue(true)       // No queue in dev by default
                .EnableMinimalMode();
        }

        /// <summary>
        /// 🏭 Production-ready configuration with full governance, telemetry, and queue enabled.
        /// </summary>
        /// <example>
        /// <code>
        /// builder.Logging.AddCerbiStream(o => o.ForProduction());
        /// </code>
        /// </example>
        public CerbiStreamOptions ForProduction()
        {
            return WithConsoleOutput(false)
                .WithTelemetryEnrichment(true)
                .WithMetadataInjection(true)
                .WithGovernanceChecks(true)
                .WithDisableQueue(false)
                .WithQueueRetries(true, 3, 200)
                .EnableFullMode();
        }

        /// <summary>
        /// 🧪 Testing configuration - governance enabled but no external dependencies.
        /// </summary>
        /// <example>
        /// <code>
        /// builder.Logging.AddCerbiStream(o => o.ForTesting());
        /// </code>
        /// </example>
        public CerbiStreamOptions ForTesting()
        {
            return WithConsoleOutput(true)
                .WithTelemetryEnrichment(false)
                .WithMetadataInjection(true)
                .WithGovernanceChecks(true)
                .WithDisableQueue(true);
        }

        /// <summary>
        /// ⚡ Maximum performance - all enrichment disabled.
        /// </summary>
        /// <example>
        /// <code>
        /// builder.Logging.AddCerbiStream(o => o.ForPerformance());
        /// </code>
        /// </example>
        public CerbiStreamOptions ForPerformance()
        {
            return EnableBenchmarkMode();
        }

        public bool ValidateLog(string profileName, Dictionary<string, object> logData)
        {
            if (GovernanceValidator is not null)
            {
                return GovernanceValidator(profileName, logData);
            }

            return true; // No validator configured; nothing to enforce.
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

        // ============================================
        // ENVIRONMENT VARIABLE CONFIGURATION
        // ============================================

        /// <summary>
        /// Configures CerbiStream from environment variables.
        /// Call this to enable zero-code configuration across environments.
        /// </summary>
        /// <remarks>
        /// Environment variables override defaults but can be further overridden by fluent methods.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Auto-configure from environment
        /// builder.Logging.AddCerbiStream(o => o.FromEnvironment());
        /// 
        /// // Environment + code overrides
        /// builder.Logging.AddCerbiStream(o => o
        ///     .FromEnvironment()
        ///     .WithGovernanceProfile("custom")); // Override env setting
        /// </code>
        /// </example>
        public CerbiStreamOptions FromEnvironment()
        {
            // Step 1: Apply mode preset if set
            var mode = CerbiStreamEnvironment.GetMode();
            if (mode != null)
            {
                _ = mode switch
                {
                    CerbiStreamMode.Development => EnableDeveloperMode(),
                    CerbiStreamMode.Production => ForProduction(),
                    CerbiStreamMode.Testing => ForTesting(),
                    CerbiStreamMode.Performance => ForPerformance(),
                    _ => this
                };
            }

            // Step 2: Apply individual overrides (these take precedence over mode)
            ApplyEnvironmentOverrides();

            return this;
        }

        /// <summary>
        /// Applies individual environment variable overrides on top of current configuration.
        /// Useful for fine-tuning after applying a preset.
        /// </summary>
        public CerbiStreamOptions ApplyEnvironmentOverrides()
        {
            // Governance
            var govEnabled = CerbiStreamEnvironment.GetBool(CerbiStreamEnvironment.GOVERNANCE_ENABLED);
            if (govEnabled.HasValue) WithGovernanceChecks(govEnabled.Value);

            var govProfile = CerbiStreamEnvironment.GetString(CerbiStreamEnvironment.GOVERNANCE_PROFILE);
            if (!string.IsNullOrEmpty(govProfile)) WithGovernanceProfile(govProfile);

            var govPath = CerbiStreamEnvironment.GetString(CerbiStreamEnvironment.GOVERNANCE_PATH);
            if (!string.IsNullOrEmpty(govPath)) WithGovernanceConfigPath(govPath);

            // Queue
            var queueEnabled = CerbiStreamEnvironment.GetBool(CerbiStreamEnvironment.QUEUE_ENABLED);
            if (queueEnabled.HasValue) WithDisableQueue(!queueEnabled.Value);

            var queueType = CerbiStreamEnvironment.GetString(CerbiStreamEnvironment.QUEUE_TYPE);
            var queueConn = CerbiStreamEnvironment.GetString(CerbiStreamEnvironment.QUEUE_CONNECTION);
            var queueName = CerbiStreamEnvironment.GetString(CerbiStreamEnvironment.QUEUE_NAME);
            if (!string.IsNullOrEmpty(queueType) && !string.IsNullOrEmpty(queueConn) && !string.IsNullOrEmpty(queueName))
            {
                WithQueue(queueType, queueConn, queueName);
            }

            var retriesEnabled = CerbiStreamEnvironment.GetBool(CerbiStreamEnvironment.QUEUE_RETRIES_ENABLED);
            var retryCount = CerbiStreamEnvironment.GetInt(CerbiStreamEnvironment.QUEUE_RETRY_COUNT);
            var retryDelay = CerbiStreamEnvironment.GetInt(CerbiStreamEnvironment.QUEUE_RETRY_DELAY_MS);
            if (retriesEnabled.HasValue || retryCount.HasValue || retryDelay.HasValue)
            {
                WithQueueRetries(
                    retriesEnabled ?? EnableQueueRetries,
                    retryCount ?? QueueRetryCount,
                    retryDelay ?? QueueRetryDelayMilliseconds);
            }

            // Encryption
            var encMode = CerbiStreamEnvironment.GetEncryptionMode();
            if (encMode.HasValue) WithEncryptionMode(encMode.Value);

            var encKey = CerbiStreamEnvironment.GetEncryptionKey();
            var encIv = CerbiStreamEnvironment.GetEncryptionIV();
            if (encKey != null && encIv != null) WithEncryptionKey(encKey, encIv);

            // Console/Telemetry
            var console = CerbiStreamEnvironment.GetBool(CerbiStreamEnvironment.CONSOLE_OUTPUT);
            if (console.HasValue) WithConsoleOutput(console.Value);

            var telemetryEnabled = CerbiStreamEnvironment.GetBool(CerbiStreamEnvironment.TELEMETRY_ENABLED);
            if (telemetryEnabled.HasValue) WithTelemetryLogging(telemetryEnabled.Value);

            var telemetryEnrich = CerbiStreamEnvironment.GetBool(CerbiStreamEnvironment.TELEMETRY_ENRICHMENT);
            if (telemetryEnrich.HasValue) WithTelemetryEnrichment(telemetryEnrich.Value);

            var metadataInject = CerbiStreamEnvironment.GetBool(CerbiStreamEnvironment.METADATA_INJECTION);
            if (metadataInject.HasValue) WithMetadataInjection(metadataInject.Value);

            // File Fallback
            var fallbackEnabled = CerbiStreamEnvironment.GetBool(CerbiStreamEnvironment.FILE_FALLBACK_ENABLED);
            var primaryPath = CerbiStreamEnvironment.GetString(CerbiStreamEnvironment.FILE_PRIMARY_PATH);
            var fallbackPath = CerbiStreamEnvironment.GetString(CerbiStreamEnvironment.FILE_FALLBACK_PATH);

            if (fallbackEnabled == true && !string.IsNullOrEmpty(fallbackPath))
            {
                WithFileFallback(
                    fallbackPath,
                    primaryPath ?? "logs/log-primary.json");
            }

            return this;
        }

        /// <summary>
        /// Checks if CerbiStream should auto-configure from environment.
        /// Returns true if CERBISTREAM_MODE or any config env vars are set.
        /// </summary>
        public static bool ShouldUseEnvironmentConfig() =>
            CerbiStreamEnvironment.HasAnyConfiguration();

        /// <summary>
        /// Gets diagnostic info about which environment variables are currently set.
        /// Useful for troubleshooting configuration issues.
        /// </summary>
        public static Dictionary<string, string> GetEnvironmentDiagnostics() =>
            CerbiStreamEnvironment.GetAllSetVariables();
    }
}
