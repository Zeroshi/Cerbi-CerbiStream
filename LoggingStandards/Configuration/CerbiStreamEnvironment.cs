using System;
using System.Collections.Generic;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

namespace CerbiStream.Logging.Configuration
{
    /// <summary>
    /// Environment variable names and parsing utilities for CerbiStream configuration.
    /// Enables zero-code configuration changes across environments.
    /// </summary>
    /// <example>
    /// Set environment variables to control behavior without code changes:
    /// <code>
    /// # Linux/Mac
    /// export CERBISTREAM_MODE=production
    /// export CERBISTREAM_QUEUE_ENABLED=true
    /// 
    /// # Windows PowerShell
    /// $env:CERBISTREAM_MODE = "production"
    /// $env:CERBISTREAM_QUEUE_ENABLED = "true"
    /// 
    /// # Docker
    /// docker run -e CERBISTREAM_MODE=production myapp
    /// 
    /// # Kubernetes
    /// env:
    ///   - name: CERBISTREAM_MODE
    ///     value: "production"
    /// </code>
    /// </example>
    public static class CerbiStreamEnvironment
    {
        // ============================================
        // ENVIRONMENT VARIABLE NAMES
        // ============================================

        /// <summary>
        /// Master mode switch: development, production, testing, performance
        /// </summary>
        public const string MODE = "CERBISTREAM_MODE";

        /// <summary>
        /// Enable/disable governance checks (true/false)
        /// </summary>
        public const string GOVERNANCE_ENABLED = "CERBISTREAM_GOVERNANCE_ENABLED";

        /// <summary>
        /// Governance profile name to use
        /// </summary>
        public const string GOVERNANCE_PROFILE = "CERBISTREAM_GOVERNANCE_PROFILE";

        /// <summary>
        /// Path to governance configuration file (already existed as CERBI_GOVERNANCE_PATH)
        /// </summary>
        public const string GOVERNANCE_PATH = "CERBI_GOVERNANCE_PATH";

        /// <summary>
        /// Enable/disable queue sending (true/false)
        /// </summary>
        public const string QUEUE_ENABLED = "CERBISTREAM_QUEUE_ENABLED";

        /// <summary>
        /// Queue provider type: AzureServiceBus, RabbitMQ, AWSSQS, Kafka, GooglePubSub, AzureQueue
        /// </summary>
        public const string QUEUE_TYPE = "CERBISTREAM_QUEUE_TYPE";

        /// <summary>
        /// Queue connection string or host
        /// </summary>
        public const string QUEUE_CONNECTION = "CERBISTREAM_QUEUE_CONNECTION";

        /// <summary>
        /// Queue/topic name
        /// </summary>
        public const string QUEUE_NAME = "CERBISTREAM_QUEUE_NAME";

        /// <summary>
        /// Enable queue retries (true/false)
        /// </summary>
        public const string QUEUE_RETRIES_ENABLED = "CERBISTREAM_QUEUE_RETRIES_ENABLED";

        /// <summary>
        /// Number of retry attempts (integer)
        /// </summary>
        public const string QUEUE_RETRY_COUNT = "CERBISTREAM_QUEUE_RETRY_COUNT";

        /// <summary>
        /// Delay between retries in milliseconds (integer)
        /// </summary>
        public const string QUEUE_RETRY_DELAY_MS = "CERBISTREAM_QUEUE_RETRY_DELAY_MS";

        /// <summary>
        /// Encryption mode: None, Base64, AES
        /// </summary>
        public const string ENCRYPTION_MODE = "CERBISTREAM_ENCRYPTION_MODE";

        /// <summary>
        /// AES encryption key (base64 encoded)
        /// </summary>
        public const string ENCRYPTION_KEY = "CERBISTREAM_ENCRYPTION_KEY";

        /// <summary>
        /// AES encryption IV (base64 encoded)
        /// </summary>
        public const string ENCRYPTION_IV = "CERBISTREAM_ENCRYPTION_IV";

        /// <summary>
        /// Enable/disable console output (true/false)
        /// </summary>
        public const string CONSOLE_OUTPUT = "CERBISTREAM_CONSOLE_OUTPUT";

        /// <summary>
        /// Enable/disable telemetry sending (true/false)
        /// </summary>
        public const string TELEMETRY_ENABLED = "CERBISTREAM_TELEMETRY_ENABLED";

        /// <summary>
        /// Enable/disable telemetry enrichment (true/false)
        /// </summary>
        public const string TELEMETRY_ENRICHMENT = "CERBISTREAM_TELEMETRY_ENRICHMENT";

        /// <summary>
        /// Enable/disable metadata injection (true/false)
        /// </summary>
        public const string METADATA_INJECTION = "CERBISTREAM_METADATA_INJECTION";

        /// <summary>
        /// Enable/disable file fallback (true/false)
        /// </summary>
        public const string FILE_FALLBACK_ENABLED = "CERBISTREAM_FILE_FALLBACK_ENABLED";

        /// <summary>
        /// Primary log file path
        /// </summary>
        public const string FILE_PRIMARY_PATH = "CERBISTREAM_FILE_PRIMARY_PATH";

        /// <summary>
        /// Fallback log file path
        /// </summary>
        public const string FILE_FALLBACK_PATH = "CERBISTREAM_FILE_FALLBACK_PATH";

        // ============================================
        // PARSING UTILITIES
        // ============================================

        /// <summary>
        /// Gets a string environment variable, returning null if not set
        /// </summary>
        public static string? GetString(string name) =>
            Environment.GetEnvironmentVariable(name);

        /// <summary>
        /// Gets a boolean environment variable (true/false, 1/0, yes/no)
        /// </summary>
        public static bool? GetBool(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) return null;

            return value.ToLowerInvariant() switch
            {
                "true" or "1" or "yes" or "on" => true,
                "false" or "0" or "no" or "off" => false,
                _ => null
            };
        }

        /// <summary>
        /// Gets an integer environment variable
        /// </summary>
        public static int? GetInt(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return int.TryParse(value, out var result) ? result : null;
        }

        /// <summary>
        /// Gets the CerbiStream mode from environment
        /// </summary>
        public static CerbiStreamMode? GetMode()
        {
            var value = Environment.GetEnvironmentVariable(MODE);
            if (string.IsNullOrWhiteSpace(value)) return null;

            return value.ToLowerInvariant() switch
            {
                "development" or "dev" => CerbiStreamMode.Development,
                "production" or "prod" => CerbiStreamMode.Production,
                "testing" or "test" => CerbiStreamMode.Testing,
                "performance" or "perf" or "benchmark" => CerbiStreamMode.Performance,
                _ => null
            };
        }

        /// <summary>
        /// Gets encryption type from environment
        /// </summary>
        public static EncryptionType? GetEncryptionMode()
        {
            var value = Environment.GetEnvironmentVariable(ENCRYPTION_MODE);
            if (string.IsNullOrWhiteSpace(value)) return null;

            return value.ToLowerInvariant() switch
            {
                "none" or "off" or "disabled" => EncryptionType.None,
                "base64" => EncryptionType.Base64,
                "aes" or "aes128" or "aes256" => EncryptionType.AES,
                _ => null
            };
        }

        /// <summary>
        /// Gets encryption key from base64-encoded environment variable
        /// </summary>
        public static byte[]? GetEncryptionKey()
        {
            var value = Environment.GetEnvironmentVariable(ENCRYPTION_KEY);
            if (string.IsNullOrWhiteSpace(value)) return null;
            try { return Convert.FromBase64String(value); }
            catch { return null; }
        }

        /// <summary>
        /// Gets encryption IV from base64-encoded environment variable
        /// </summary>
        public static byte[]? GetEncryptionIV()
        {
            var value = Environment.GetEnvironmentVariable(ENCRYPTION_IV);
            if (string.IsNullOrWhiteSpace(value)) return null;
            try { return Convert.FromBase64String(value); }
            catch { return null; }
        }

        /// <summary>
        /// Checks if any CerbiStream environment variables are set
        /// </summary>
        public static bool HasAnyConfiguration()
        {
            return GetMode() != null
                || GetBool(GOVERNANCE_ENABLED) != null
                || GetString(GOVERNANCE_PROFILE) != null
                || GetBool(QUEUE_ENABLED) != null
                || GetString(QUEUE_TYPE) != null
                || GetBool(CONSOLE_OUTPUT) != null
                || GetBool(TELEMETRY_ENABLED) != null
                || GetEncryptionMode() != null
                || GetBool(FILE_FALLBACK_ENABLED) != null;
        }

        /// <summary>
        /// Returns all currently set CerbiStream environment variables for diagnostics
        /// </summary>
        public static Dictionary<string, string> GetAllSetVariables()
        {
            var result = new Dictionary<string, string>();
            var names = new[]
            {
                MODE, GOVERNANCE_ENABLED, GOVERNANCE_PROFILE, GOVERNANCE_PATH,
                QUEUE_ENABLED, QUEUE_TYPE, QUEUE_CONNECTION, QUEUE_NAME,
                QUEUE_RETRIES_ENABLED, QUEUE_RETRY_COUNT, QUEUE_RETRY_DELAY_MS,
                ENCRYPTION_MODE, ENCRYPTION_KEY, ENCRYPTION_IV,
                CONSOLE_OUTPUT, TELEMETRY_ENABLED, TELEMETRY_ENRICHMENT, METADATA_INJECTION,
                FILE_FALLBACK_ENABLED, FILE_PRIMARY_PATH, FILE_FALLBACK_PATH
            };

            foreach (var name in names)
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrEmpty(value))
                {
                    // Mask sensitive values
                    if (name.Contains("KEY") || name.Contains("IV") || name.Contains("CONNECTION"))
                        result[name] = "***SET***";
                    else
                        result[name] = value;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// CerbiStream operating modes
    /// </summary>
    public enum CerbiStreamMode
    {
        /// <summary>Console output, governance enabled, queue disabled</summary>
        Development,
        /// <summary>No console, full governance, queue enabled, telemetry enabled</summary>
        Production,
        /// <summary>Console output, governance enabled, queue disabled</summary>
        Testing,
        /// <summary>All features disabled for maximum performance</summary>
        Performance
    }
}
