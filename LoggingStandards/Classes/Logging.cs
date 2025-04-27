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
    public class Logging : IBaseLogging
    {
        private readonly ILogger<Logging> _logger;
        private readonly ISendMessage _queue;
        private readonly IConvertToJson _jsonConverter;
        private readonly IEncryption _encryption;
        private readonly CerbiStreamOptions _options;

        public Logging(
            ILogger<Logging> logger,
            ISendMessage queue,
            IConvertToJson jsonConverter,
            IEncryption encryption,
            CerbiStreamOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _jsonConverter = jsonConverter ?? throw new ArgumentNullException(nameof(jsonConverter));
            _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
            _options = options ?? new CerbiStreamOptions();
        }

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
            {
                metadata["Environment"] = environment.Name ?? "Unknown";
            }

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
                _logger.LogError("[CerbiStream] Governance validation failed; dropping log.");
                return Task.FromResult(false);
            }

            var entry = new
            {
                ApplicationMessage = applicationMessage,
                CurrentMethod = currentMethod,
                LogLevel = logLevel,
                Log = log, // freeform log message stays OUTSIDE metadata
                Metadata = metadata
            };

            return SendLogAsync(entry);
        }

        public Task<bool> LogEventAsync(string message, LogLevel logLevel, Dictionary<string, object>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("Log message is empty or null.");
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

            var entry = new { Message = message, Metadata = metadata };
            return SendLogAsync(entry);
        }

        public Task<bool> LogPerformanceAsync(string eventName, long elapsedMilliseconds, Dictionary<string, object>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(eventName) || elapsedMilliseconds < 0)
            {
                _logger.LogWarning("Invalid performance log parameters.");
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
                    _logger.LogDebug($"[CerbiStream] Payload for log ID {logId} encrypted ({_options.EncryptionMode}).");
                }

                _logger.LogInformation($"[CerbiStream] Sending log ID {logId}...");

                if (_options.DisableQueueSending)
                {
                    _logger.LogInformation($"[CerbiStream] Queue send disabled; log ID {logId} dropped.");
                    return true;
                }

                if (_options.EnableQueueRetries)
                {
                    var policy = Policy
                        .Handle<Exception>()
                        .WaitAndRetryAsync(
                            _options.QueueRetryCount,
                            idx => TimeSpan.FromMilliseconds(_options.QueueRetryDelayMilliseconds),
                            (ex, span, retry, ctx) => _logger.LogWarning(ex, $"Retry {retry} failed for log ID {logId}.")
                        );
                    var sentWithRetry = await policy.ExecuteAsync(() => _queue.SendMessageAsync(payload, logId));
                    return sentWithRetry || (_options.EncryptionMode != IEncryptionTypeProvider.EncryptionType.None && _encryption.IsEnabled);
                }

                var sentNoRetry = await _queue.SendMessageAsync(payload, logId);
                return sentNoRetry || (_options.EncryptionMode != IEncryptionTypeProvider.EncryptionType.None && _encryption.IsEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logging failed.");
                return false;
            }
        }

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

            // ✅ Only enrich tracing if allowed
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
