using CerbiClientLogging.Classes;
using CerbiClientLogging.Interfaces;
using CerbiStream.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CerbiClientLogging.Implementations
{
    public class Logging : IBaseLogging
    {
        private readonly ILogger<Logging> _logger;
        private readonly IQueue _queue; // ✅ Injected from Fluent API
        private readonly ConvertToJson _jsonConverter;
        private readonly IEncryption _encryption;

        public Logging(
            ILogger<Logging> logger,
            IQueue queue,  // ✅ Replace _transactionDestination
            IConvertToJson jsonConverter,
            IEncryption encryption)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue)); // ✅ Uses Fluent API
            _jsonConverter = (jsonConverter ?? throw new ArgumentNullException(nameof(jsonConverter))) as ConvertToJson;
            _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        }

        public async Task<bool> SendApplicationLogAsync(
            string applicationMessage, string currentMethod, LogLevel logLevel,
            string log, string? applicationName, string? platform, bool? onlyInnerException,
            string? note, Exception? error, ITransactionDestination? transactionDestination,
            TransactionDestinationTypes? transactionDestinationTypes, IEncryption? encryption,
            IEnvironment? environment, IIdentifiableInformation? identifiableInformation,
            string? payload, string? cloudProvider, string? instanceId, string? applicationVersion,
            string? region, string? requestId)
        {
            var metadata = new Dictionary<string, object>
                {
                    { "CloudProvider", cloudProvider ?? "Unknown" },
                    { "Region", region ?? "Unknown" },
                    { "InstanceId", instanceId ?? "Unknown" },
                    { "ApplicationVersion", applicationVersion ?? "Unknown" },
                    { "RequestId", requestId ?? Guid.NewGuid().ToString() },
                    { "Log", log },
                    { "Platform", platform ?? "Unknown" },
                    { "OnlyInnerException", onlyInnerException ?? false },
                    { "Note", note ?? "No note" },
                    { "Error", error?.Message ?? "No Error" },

                };

            return await SendLog(new
            {
                ApplicationMessage = applicationMessage,
                CurrentMethod = currentMethod,
                LogLevel = logLevel,
                Metadata = metadata
            });
        }

        private async Task<bool> SendLog(object logEntry)
        {
            try
            {
                string formattedLog = _jsonConverter.ConvertMessageToJson(logEntry);

                // ✅ Send directly to the chosen queue
                await _queue.SendMessageAsync(formattedLog, Guid.NewGuid());

                _logger.LogInformation($"[{_queue.GetType().Name}] Log Sent.");
                return true;
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
        }

        private void EncryptMetadata(Dictionary<string, object> metadata)
        {
            if (_encryption.IsEnabled)
            {
                foreach (var key in new List<string> { "APIKey", "SensitiveField" })
                {
                    if (metadata.ContainsKey(key) && metadata[key] is string value)
                    {
                        metadata[key] = _encryption.Encrypt(value);
                    }
                }
            }
        }

        public async Task<bool> LogEventAsync(string message, LogLevel logLevel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("Log message is empty or null.");
                    return false;
                }

                var logEntry = new
                {
                    TimestampUtc = DateTime.UtcNow,
                    LogLevel = logLevel,
                    Message = message
                };

                string formattedLog = _jsonConverter.ConvertMessageToJson(logEntry);

                if (_queue == null)
                {
                    _logger.LogError("Queue is null. Cannot send log.");
                    return false;
                }

                bool result = await _queue.SendMessageAsync(formattedLog, Guid.NewGuid());

                if (!result)
                {
                    _logger.LogError("Queue failed to send message.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logging failed.");
                return false;
            }
        }


        public async Task<bool> LogPerformanceAsync(string eventName, long elapsedMilliseconds, Dictionary<string, object>? metadata = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eventName) || elapsedMilliseconds < 0)
                {
                    _logger.LogWarning("Invalid performance log parameters.");
                    return false;
                }

                metadata ??= new Dictionary<string, object>();
                metadata["ElapsedMilliseconds"] = elapsedMilliseconds;
                metadata["EventName"] = eventName;

                var logEntry = new
                {
                    EventName = eventName,
                    ElapsedMilliseconds = elapsedMilliseconds,
                    Metadata = metadata
                };

                string formattedLog = _jsonConverter.ConvertMessageToJson(logEntry);

                if (_queue == null)
                {
                    _logger.LogError("Queue is null. Cannot send performance log.");
                    return false;
                }

                bool result = await _queue.SendMessageAsync(formattedLog, Guid.NewGuid());

                if (!result)
                {
                    _logger.LogError("Failed to log performance data.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Performance logging failed.");
                return false;
            }
        }



        public async Task<bool> LogEventAsync(string message, LogLevel logLevel, Dictionary<string, object>? metadata = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("Log message is empty or null.");
                    return false;
                }

                metadata ??= new Dictionary<string, object>();
                metadata["TimestampUtc"] = DateTime.UtcNow;
                metadata["LogLevel"] = logLevel.ToString();

                var logEntry = new
                {
                    Message = message,
                    Metadata = metadata
                };

                string formattedLog = _jsonConverter.ConvertMessageToJson(logEntry);

                if (_queue == null)
                {
                    _logger.LogError("Queue is null. Cannot send log.");
                    return false;
                }

                bool result = await _queue.SendMessageAsync(formattedLog, Guid.NewGuid());

                if (!result)
                {
                    _logger.LogError("Queue failed to send message.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logging failed.");
                return false;
            }
        }

    }
}
