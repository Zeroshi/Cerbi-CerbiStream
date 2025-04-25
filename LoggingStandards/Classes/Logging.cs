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
                ["Log"] = log,
                ["Platform"] = platform ?? "Unknown",
                ["OnlyInnerException"] = onlyInnerException ?? false,
                ["Note"] = note ?? "No note",
                ["Error"] = error?.Message ?? "No Error"
            };

            if (_options.EnableMetadataInjection)
            {
                EnrichMetadata(metadata);
                EncryptMetadata(metadata);
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
                EncryptMetadata(metadata);
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
                EncryptMetadata(metadata);
            }

            // Pass performance details as metadata inside the main log entry
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

                // Serialize JSON payload
                string payload = _jsonConverter.ConvertMessageToJson(new
                {
                    LogId = logId,
                    LogData = logEntry
                });

                // Apply full payload encryption
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

        // Adds timestamp
        private void EnrichMetadata(Dictionary<string, object> metadata)
            => metadata.TryAdd("TimestampUtc", DateTime.UtcNow);

        // Encrypts selected sensitive metadata fields
        private void EncryptMetadata(Dictionary<string, object> metadata)
        {
            if (!_encryption.IsEnabled) return;

            foreach (var key in new List<string> { "APIKey", "SensitiveField" })
            {
                if (metadata.TryGetValue(key, out var val) && val is string str)
                    metadata[key] = _encryption.Encrypt(str);
            }
        }
    }
}
