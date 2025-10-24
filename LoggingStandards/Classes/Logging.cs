using Cerbi.Governance;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using CerbiStream.Classes;
using CerbiStream.Interfaces;
using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CerbiClientLogging.Implementations
{
    /// <summary>
    /// The <c>Logging</c> class is an implementation of the <c>IBaseLogging</c> interface,
    /// designed for handling structured logging, including application-level logs, events, and performance metrics.
    /// </summary>
    /// <remarks>
    /// This class integrates various components such as message queuing, JSON conversion,
    /// encryption, and runtime governance validation to support robust logging mechanisms.
    /// </remarks>
    /// <example>
    /// This class should be instantiated with required dependencies such as <c>ISendMessage</c>,
    /// <c>IConvertToJson</c>, <c>IEncryption</c>, and <c>CerbiStreamOptions</c>. Optional parameters
    /// like <c>RuntimeGovernanceValidator</c> can be used to extend its behavior.
    /// </example>
    /// <param name="queue">
    /// Instance of <c>ISendMessage</c> to handle the dispatch of log messages.
    /// </param>
    /// <param name="jsonConverter">
    /// Instance of <c>IConvertToJson</c> to handle serialization of log messages into JSON.
    /// </param>
    /// <param name="encryption">
    /// Instance of <c>IEncryption</c> for encrypting sensitive log data.
    /// </param>
    /// <param name="options">
    /// Configuration options encapsulated in a <c>CerbiStreamOptions</c> instance.
    /// </param>
    /// <param name="governanceValidator">
    /// Optional instance of <c>RuntimeGovernanceValidator</c> for applying runtime governance rules.
    /// </param>
    /// <method name="SendApplicationLogAsync">
    /// Sends an application-specific log entry with an extensive set of attributes for detailed logging.
    /// </method>
    /// <method name="LogEventAsync">
    /// Records a general logging event with optional metadata for contextual information.
    /// </method>
    /// <method name="LogPerformanceAsync">
    /// Logs performance metrics, capturing event name and elapsed time in milliseconds, with optional metadata.
    /// </method>
    public class Logging : IBaseLogging
    {
        /// <summary>
        /// Represents the queue component used for sending log messages asynchronously.
        /// This variable is a dependency injected instance of <see cref="ISendMessage"/>.
        /// It is used to offload log data (in the form of payloads and log IDs) to the respective destination or service.
        /// </summary>
        private readonly ISendMessage _queue;

        /// <summary>
        /// Represents a converter responsible for transforming objects into their JSON representation.
        /// This variable is utilized within the logging implementation to serialize log data into JSON
        /// format before further processing, such as encryption or sending to a message queue.
        /// </summary>
        private readonly IConvertToJson _jsonConverter;

        /// <summary>
        /// Represents the encryption mechanism used within the logging implementation.
        /// This field provides functionality to encrypt and decrypt sensitive log data
        /// as well as determine encryption capabilities and the type of encryption applied.
        /// </summary>
        /// <remarks>
        /// The <see cref="IEncryption"/> implementation is used for secure handling
        /// of sensitive information when encryption is required during the logging process.
        /// It determines whether encryption is enabled, applies encryption and decryption
        /// to data, and specifies the encryption method being utilized.
        /// </remarks>
        /// <seealso cref="CerbiClientLogging.Interfaces.IEncryption"/>
        private readonly IEncryption _encryption;

        /// <summary>
        /// Configuration options used for logging functionality.
        /// The _options variable is an instance of the <see cref="CerbiStreamOptions"/> class,
        /// which provides behavior configurations and logging-related properties.
        /// </summary>
        private readonly CerbiStreamOptions _options;

        /// <summary>
        /// Represents an optional runtime governance validator utilized to enforce and validate governance rules
        /// on log metadata or related operations.
        /// </summary>
        private readonly RuntimeGovernanceValidator? _governanceValidator;

        /// <summary>
        /// Represents the primary implementation of logging functionality.
        /// </summary>
        /// <remarks>
        /// The <see cref="Logging"/> class is responsible for handling event logging by
        /// utilizing external services and configurations, such as message queues,
        /// JSON conversion, encryption, and runtime governance validation.
        /// </remarks>
        public Logging(
            ISendMessage queue,
            IConvertToJson jsonConverter,
            IEncryption encryption,
            CerbiStreamOptions options,
            RuntimeGovernanceValidator? governanceValidator = null)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _jsonConverter = jsonConverter ?? throw new ArgumentNullException(nameof(jsonConverter));
            _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
            _options = options ?? new CerbiStreamOptions();
            _governanceValidator = governanceValidator;
        }

        /// Asynchronously sends an application log with specified parameters and metadata.
        /// <param name="applicationMessage">The main log message or description of the event being logged.</param>
        /// <param name="currentMethod">The name of the method from which the log is being sent.</param>
        /// <param name="logLevel">The severity level of the log message.</param>
        /// <param name="log">The raw log information.</param>
        /// <param name="applicationName">The name of the application generating the log.</param>
        /// <param name="platform">The platform or environment on which the application is running.</param>
        /// <param name="onlyInnerException">Indicates whether only the inner exception details should be logged, if applicable.</param>
        /// <param name="note">An optional note providing additional context for the log.</param>
        /// <param name="error">The exception associated with the log, if any.</param>
        /// <param name="transactionDestination">The destination object representing where the transaction log should be sent.</param>
        /// <param name="transactionDestinationTypes">The type of destination system for the transaction log.</param>
        /// <param name="encryption">Optional encryption service that can secure sensitive log data.</param>
        /// <param name="environment">The environment settings in which the log is being generated.</param>
        /// <param name="identifiableInformation">Identifiable information that might be included in the log.</param>
        /// <param name="payload">The payload or additional information associated with the log.</param>
        /// <param name="cloudProvider">The cloud provider hosting the application (e.g., AWS, Azure, Google Cloud).</param>
        /// <param name="instanceId">The instance ID of the application or its host environment.</param>
        /// <param name="applicationVersion">The version of the application generating the log.</param>
        /// <param name="region">The geographical region in which the application or service is running.</param>
        /// <param name="requestId">The unique identifier for the request associated with the log, typically used for tracing.</param>
        /// <return>Returns a task that represents the asynchronous operation, containing a boolean indicating whether the log was successfully sent.</return>
        public Task<bool> SendApplicationLogAsync(
            string applicationMessage,
            string currentMethod,
            LogLevel logLevel,
            string log,
            string? applicationName,
            string? platform,
            bool? onlyInnerException,
            string? note,
            Exception? error,
            ITransactionDestination? transactionDestination,
            TransactionDestinationTypes? transactionDestinationTypes,
            IEncryption? encryption,
            IEnvironment? environment,
            IIdentifiableInformation? identifiableInformation,
            string? payload,
            string? cloudProvider,
            string? instanceId,
            string? applicationVersion,
            string? region,
            string? requestId)
        {
            var metadata = new Dictionary<string, object>
            {
                ["CloudProvider"] = cloudProvider ?? "Unknown",
                ["Region"] = region ?? "Unknown",
                ["InstanceId"] = instanceId ?? "Unknown",
                ["ApplicationVersion"] = applicationVersion ?? "Unknown",
                ["RequestId"] = requestId ?? Guid.NewGuid().ToString(),
                ["Platform"] = platform ?? "Unknown",
                ["OnlyInnerException"] = onlyInnerException ?? false,
                ["Note"] = note ?? "No note"
            };

            if (environment != null && !metadata.ContainsKey("Environment"))
                metadata["Environment"] = environment.Name ?? "Unknown";

            if (error != null && !metadata.ContainsKey("ErrorCode"))
            {
                metadata["ErrorCode"] = error.HResult.ToString();
                metadata["TransactionStatus"] = "Failed";
            }

            if (_options.EnableMetadataInjection)
            {
                EnrichMetadata(metadata);
                EncryptInternalSecrets(metadata);
            }

            if (!_options.ValidateLog(_options.QueueType, metadata))
            {
                Console.WriteLine("[CerbiStream] Governance validation failed; dropping log.");
                return Task.FromResult(false);
            }

            var entry = new
            {
                ApplicationMessage = applicationMessage,
                CurrentMethod = currentMethod,
                LogLevel = logLevel,
                Log = log,
                Metadata = metadata
            };

            return SendLogAsync(entry);
        }

        /// <summary>
        /// Asynchronously logs an event message with the specified log level and optional metadata.
        /// </summary>
        /// <param name="message">
        /// The log message to record. Cannot be null, empty, or whitespace.
        /// </param>
        /// <param name="logLevel">
        /// The severity level of the log message.
        /// </param>
        /// <param name="metadata">
        /// Optional metadata to enrich the log. If null, a new dictionary will be created and populated with default values.
        /// </param>
        /// <returns>
        /// A task that resolves to a boolean indicating whether the log event was successfully recorded.
        /// </returns>
        public Task<bool> LogEventAsync(string message, LogLevel logLevel, Dictionary<string, object>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine("[CerbiStream] Log message is empty or null.");
                return Task.FromResult(false);
            }

            metadata ??= new Dictionary<string, object>();
            metadata["TimestampUtc"] = DateTime.UtcNow;
            metadata["LogLevel"] = logLevel.ToString();

            if (_options.EnableMetadataInjection)
            {
                EnrichMetadata(metadata);
                EncryptInternalSecrets(metadata);
            }

            if (_governanceValidator != null)
            {
                _governanceValidator.ValidateInPlace(metadata);
            }


            var entry = new { Message = message, Metadata = metadata };
            return SendLogAsync(entry);
        }

        /// <summary>
        /// Logs performance metrics for a specific event with optional metadata.
        /// </summary>
        /// <param name="eventName">The name of the event being logged.</param>
        /// <param name="elapsedMilliseconds">The elapsed time in milliseconds for the event.</param>
        /// <param name="metadata">Optional metadata in the form of a dictionary, which may include additional information about the event.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the log operation was successful.</returns>
        public Task<bool> LogPerformanceAsync(string eventName, long elapsedMilliseconds, Dictionary<string, object>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(eventName) || elapsedMilliseconds < 0)
            {
                Console.WriteLine("[CerbiStream] Invalid performance log parameters.");
                return Task.FromResult(false);
            }

            metadata ??= new Dictionary<string, object>();
            metadata["ElapsedMilliseconds"] = elapsedMilliseconds;
            metadata["EventName"] = eventName;

            if (_options.EnableMetadataInjection)
            {
                EnrichMetadata(metadata);
                EncryptInternalSecrets(metadata);
            }

            var entry = new
            {
                Message = eventName,
                Metadata = metadata
            };

            return SendLogAsync(entry);
        }

        /// <summary>
        /// Sends a log entry asynchronously to the logging queue or system.
        /// </summary>
        /// <param name="logEntry">The log entry object containing log details and metadata to be sent.</param>
        /// <returns>A task representing the asynchronous operation, containing a boolean value indicating
        /// whether the log entry was successfully sent.</returns>
        private async Task<bool> SendLogAsync(object logEntry)
        {
            try
            {
                var logId = Guid.NewGuid().ToString();

                string payload = _jsonConverter.ConvertMessageToJson(new
                {
                    LogId = logId,
                    LogData = logEntry
                });

                if (_options.EncryptionMode != IEncryptionTypeProvider.EncryptionType.None && _encryption.IsEnabled)
                {
                    payload = _encryption.Encrypt(payload);
                    Console.WriteLine($"[CerbiStream] Payload for log ID {logId} encrypted ({_options.EncryptionMode}).");
                }

                Console.WriteLine($"[CerbiStream] Sending log ID {logId}...");

                if (_options.DisableQueueSending)
                {
                    Console.WriteLine($"[CerbiStream] Queue send disabled; log ID {logId} dropped.");
                    return true;
                }

                if (_options.EnableQueueRetries)
                {
                    var policy = Policy
                        .Handle<Exception>()
                        .WaitAndRetryAsync(
                            _options.QueueRetryCount,
                            idx => TimeSpan.FromMilliseconds(_options.QueueRetryDelayMilliseconds),
                            (ex, span, retry, ctx) =>
                            {
                                Console.WriteLine($"[CerbiStream] Retry {retry} failed for log ID {logId}. Error: {ex.Message}");
                            });

                    var sentWithRetry = await policy.ExecuteAsync(() => _queue.SendMessageAsync(payload, logId));
                    return sentWithRetry || (_options.EncryptionMode != IEncryptionTypeProvider.EncryptionType.None && _encryption.IsEnabled);
                }

                var sentNoRetry = await _queue.SendMessageAsync(payload, logId);
                return sentNoRetry || (_options.EncryptionMode != IEncryptionTypeProvider.EncryptionType.None && _encryption.IsEnabled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CerbiStream] Logging failed. Error: {ex.Message}");
                return false;
            }
        }

        /// Enriches the provided metadata dictionary with additional context, such as timestamps,
        /// service information, and tracing details, if available. Updates are made in place.
        /// <param name="metadata">
        /// A dictionary containing metadata to be enriched. This may include logging and tracing properties.
        /// </param>
        private void EnrichMetadata(Dictionary<string, object> metadata)
        {
            metadata.TryAdd("TimestampUtc", DateTime.UtcNow);

            if (!metadata.ContainsKey("ServiceName") && !string.IsNullOrEmpty(_options.ServiceName))
                metadata["ServiceName"] = _options.ServiceName;

            if (!metadata.ContainsKey("OriginApp") && !string.IsNullOrEmpty(_options.OriginApp))
                metadata["OriginApp"] = _options.OriginApp;

            if (!metadata.ContainsKey("ApplicationType") && !string.IsNullOrEmpty(_options.ApplicationType))
                metadata["ApplicationType"] = _options.ApplicationType;

            if (!metadata.ContainsKey("ServiceType") && !string.IsNullOrEmpty(_options.ServiceType))
                metadata["ServiceType"] = _options.ServiceType;

            if (!metadata.ContainsKey("TargetApplicationType") && !string.IsNullOrEmpty(_options.TargetApplicationType))
                metadata["TargetApplicationType"] = _options.TargetApplicationType;

            if (!metadata.ContainsKey("TargetServiceType") && !string.IsNullOrEmpty(_options.TargetServiceType))
                metadata["TargetServiceType"] = _options.TargetServiceType;

            if (!_options.MinimalMode && _options.EnableTracingEnrichment && System.Diagnostics.Activity.Current != null)
            {
                var activity = System.Diagnostics.Activity.Current;

                if (!metadata.ContainsKey("TraceId") && activity.TraceId != default)
                    metadata["TraceId"] = activity.TraceId.ToString();

                if (!metadata.ContainsKey("SpanId") && activity.SpanId != default)
                    metadata["SpanId"] = activity.SpanId.ToString();

                if (!metadata.ContainsKey("ParentSpanId") && activity.ParentSpanId != default)
                    metadata["ParentSpanId"] = activity.ParentSpanId.ToString();
            }
        }

        /// Encrypts sensitive internal secrets within the provided metadata dictionary.
        /// <param name="metadata">A dictionary containing metadata information. Sensitive data, such as API keys, will be encrypted if encryption is enabled.</param>
        private void EncryptInternalSecrets(Dictionary<string, object> metadata)
        {
            if (!_encryption.IsEnabled) return;

            if (metadata.TryGetValue("APIKey", out var val) && val is string str)
            {
                metadata["APIKey"] = _encryption.Encrypt(str);
            }
        }
    }
}
