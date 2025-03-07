using CerbiClientLogging.Classes;
using CerbiClientLogging.Interfaces;
using CerberusLogging.Classes.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CerbiClientLogging.Implementations
{
    public class Logging : IBaseLogging
    {
        private readonly ILogger<Logging> _logger;
        private readonly ITransactionDestination _transactionDestination;
        private readonly ConvertToJson _jsonConverter;
        private readonly IEncryption _encryption;

        public Logging(
            ILogger<Logging> logger,
            ITransactionDestination transactionDestination,
            ConvertToJson jsonConverter,
            IEncryption encryption)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transactionDestination = transactionDestination ?? throw new ArgumentNullException(nameof(transactionDestination));
            _jsonConverter = jsonConverter ?? throw new ArgumentNullException(nameof(jsonConverter));
            _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        }

        public async Task<bool> LogEventAsync(
            string message,
            LogLevel logLevel,
            Dictionary<string, object>? metadata = null)
        {
            metadata ??= new Dictionary<string, object>();
            EnrichMetadata(metadata);
            EncryptMetadata(metadata);

            var logEntry = new
            {
                TimestampUtc = DateTime.UtcNow,
                LogLevel = logLevel,
                Message = message,
                Metadata = metadata
            };

            return await SendLog(logEntry);
        }

        public async Task<bool> SendApplicationLogAsync(
            string applicationMessage,
            string currentMethod,
            LogLevel logLevel,
            string log,
            string? applicationName = null,
            string? platform = null,
            bool? onlyInnerException = null,
            string? note = null,
            Exception? error = null,
            ITransactionDestination? transactionDestination = null,
            TransactionDestinationTypes? transactionDestinationTypes = null,
            IEncryption? encryption = null,
            IEnvironment? environment = null,
            IIdentifiableInformation? identifiableInformation = null,
            string? payload = null,
            string? cloudProvider = null,
            string? instanceId = null,
            string? applicationVersion = null,
            string? region = null,
            string? requestId = null)
        {
            if (string.IsNullOrEmpty(applicationMessage) || string.IsNullOrEmpty(currentMethod))
            {
                _logger.LogWarning("Invalid log message or method name.");
                return false;
            }

            try
            {
                var metadata = new Dictionary<string, object>
                {
                    { "CloudProvider", cloudProvider ?? ApplicationMetadata.CloudProvider },
                    { "Region", region ?? ApplicationMetadata.Region },
                    { "InstanceId", instanceId ?? ApplicationMetadata.InstanceId },
                    { "ApplicationVersion", applicationVersion ?? ApplicationMetadata.ApplicationVersion },
                    { "RequestId", requestId ?? Guid.NewGuid().ToString() },
                    { "Log", log },
                    { "Platform", platform ?? "Unknown" },
                    { "OnlyInnerException", onlyInnerException ?? false },
                    { "Note", note ?? "No note" },
                    { "Error", error?.Message ?? "No Error" }
                };

                EnrichMetadata(metadata);
                EncryptMetadata(metadata);

                return await LogEventAsync(applicationMessage, logLevel, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logging failed.");
                return false;
            }
        }

        public async Task<bool> LogPerformanceAsync(
            string eventName,
            long elapsedMilliseconds,
            Dictionary<string, object>? metadata = null)
        {
            metadata ??= new Dictionary<string, object>
            {
                { "Event", eventName },
                { "ElapsedTimeMs", elapsedMilliseconds }
            };

            EnrichMetadata(metadata);
            EncryptMetadata(metadata);

            return await LogEventAsync($"Performance: {eventName}", LogLevel.Information, metadata);
        }

        private void EnrichMetadata(Dictionary<string, object> metadata)
        {
            metadata.TryAdd("ApplicationId", ApplicationMetadata.ApplicationId);
            metadata.TryAdd("DeploymentType", "Cloud");
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

        private async Task<bool> SendLog(object logEntry)
        {
            try
            {
                string formattedLog = _jsonConverter.ConvertMessageToJson(logEntry);
                await _transactionDestination.SendLogAsync(formattedLog, TransactionDestinationTypes.Other);
                _logger.LogInformation($"Log Sent: {formattedLog}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logging failed.");
                return false;
            }
        }
    }
}
