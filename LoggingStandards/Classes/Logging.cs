using CerberusClientLogging.Classes;
using CerberusClientLogging.Interfaces;
using CerberusLogging.Classes.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CerberusClientLogging.Implementations
{
    public class Logging : IBaseLogging
    {
        private readonly ILogger<Logging> _logger;
        private readonly ITransactionDestination _transactionDestination;
        private readonly ConvertToJson _jsonConverter;
        private readonly IEncryption _encryption;
        private readonly bool _enableEncryption;

        public Logging(
            ILogger<Logging> logger,
            ITransactionDestination transactionDestination,
            ConvertToJson jsonConverter,
            IEncryption encryption,
            bool enableEncryption = true) // Default: Encryption ON
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transactionDestination = transactionDestination ?? throw new ArgumentNullException(nameof(transactionDestination));
            _jsonConverter = jsonConverter ?? throw new ArgumentNullException(nameof(jsonConverter));
            _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
            _enableEncryption = enableEncryption;
        }

        public async Task<bool> LogEventAsync(
            string message,
            LogLevel logLevel,
            Dictionary<string, object>? metadata = null)
        {
            metadata ??= new Dictionary<string, object>();
            EnrichMetadata(metadata);
            if (_enableEncryption) EncryptMetadata(metadata);

            var logEntry = new
            {
                TimestampUtc = DateTime.UtcNow,
                LogLevel = logLevel,
                Message = message,
                Metadata = metadata
            };

            return await SendLog(logEntry);
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

            return await LogEventAsync($"Performance: {eventName}", LogLevel.Information, metadata);
        }

        public async Task<bool> SendApplicationLogAsync(
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
            if (string.IsNullOrEmpty(applicationMessage) || string.IsNullOrEmpty(currentMethod))
            {
                _logger.LogWarning("Invalid log message or method name.");
                return false;
            }

            try
            {
                var metadata = new Dictionary<string, object>
                {
                    { "CloudProvider", cloudProvider ?? GetCloudProvider() },
                    { "InstanceId", instanceId ?? Environment.MachineName },
                    { "ApplicationVersion", applicationVersion ?? "Unknown" },
                    { "Region", region ?? GetRegion() },
                    { "RequestId", requestId ?? Guid.NewGuid().ToString() },
                    { "Log", log },
                    { "Platform", platform ?? "Unknown" },
                    { "OnlyInnerException", onlyInnerException ?? false },
                    { "Note", note ?? "No note" },
                    { "Error", error?.Message ?? "No Error" }
                };

                EnrichMetadata(metadata);
                if (_enableEncryption) EncryptMetadata(metadata);

                return await LogEventAsync(applicationMessage, logLevel, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logging failed.");
                return false;
            }
        }

        private void EnrichMetadata(Dictionary<string, object> metadata)
        {
            metadata.TryAdd("ApplicationId", "CerbiApp123");
            metadata.TryAdd("ApplicationVersion", "1.0.0");
            metadata.TryAdd("DeploymentType", GetDeploymentType());
            metadata.TryAdd("AppStartTime", GetAppStartTime());
            metadata.TryAdd("Uptime", GetUptime());
        }

        private void EncryptMetadata(Dictionary<string, object> metadata)
        {
            foreach (var key in metadata.Keys)
            {
                if (metadata[key] is string value && !string.IsNullOrWhiteSpace(value))
                {
                    metadata[key] = _encryption.Encrypt(value); // ✅ Encrypt all fields
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

        private string GetCloudProvider()
        {
            if (Environment.GetEnvironmentVariable("AWS_EXECUTION_ENV") != null)
                return "AWS";
            if (Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") != null)
                return "GCP";
            if (Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null)
                return "Azure";
            return "On-Prem";
        }

        private string GetRegion() => "us-east-1";
        private string GetDeploymentType() => "Cloud";
        private string GetAppStartTime() => DateTime.UtcNow.AddMinutes(-15).ToString();
        private string GetUptime() => $"{Environment.TickCount / 1000}s";
    }
}
