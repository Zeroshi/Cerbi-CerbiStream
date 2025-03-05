using CerberusClientLogging.Classes;
using CerberusClientLogging.Classes.ClassTypes;
using CerberusClientLogging.Interfaces;
using CerberusLogging.Classes.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace CerberusClientLogging.Implementations
{
    public class Logging : IBaseLogging
    {
        private readonly ILogger<Logging> _logger;
        private readonly ITransactionDestination _transactionDestination;
        private readonly ConvertToJson _jsonConverter;

        public Logging(
            ILogger<Logging> logger,
            ITransactionDestination transactionDestination,
            ConvertToJson jsonConverter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transactionDestination = transactionDestination ?? throw new ArgumentNullException(nameof(transactionDestination));
            _jsonConverter = jsonConverter ?? throw new ArgumentNullException(nameof(jsonConverter));
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
            string? payload)
        {
            if (string.IsNullOrWhiteSpace(applicationMessage) || string.IsNullOrWhiteSpace(currentMethod))
            {
                _logger.LogWarning("Invalid log message or method name.");
                return false;
            }

            try
            {
                var entityBase = new EntityBase
                {
                    MessageId = Guid.NewGuid(),
                    TimeStamp = DateTime.UtcNow,
                    LogLevel = logLevel,
                    Application_Name = applicationName ?? "Unknown",
                    Log = log ?? "No log provided",
                    Platform = platform ?? "Unknown Platform",
                    OnlyInnerException = onlyInnerException ?? false,
                    Note = note ?? "No note",
                    Error = error,
                    Encryption = encryption != null ? ConvertEncryption(encryption) : MetaData.Encryption.Unecrypted,
                    Environment = environment != null ? ConvertEnvironment(environment) : MetaData.Environment.NotAvailable,
                    IdentifiableInformation = identifiableInformation != null ? ConvertIdentifiableInformation(identifiableInformation) : null,
                    Payload = payload ?? "No payload"
                };

                string formattedLog = _jsonConverter.ConvertMessageToJson(entityBase);
                await _transactionDestination.SendLogAsync(formattedLog, transactionDestinationTypes ?? TransactionDestinationTypes.NotAvailable);

                _logger.LogInformation("Log successfully sent to transaction destination.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logging failed.");
                return false;
            }
        }

        private static MetaData.Encryption ConvertEncryption(IEncryption encryption) =>
            encryption.IsEnabled ? MetaData.Encryption.Encrypted : MetaData.Encryption.Unecrypted;

        private static MetaData.Environment ConvertEnvironment(IEnvironment environment) =>
            Enum.TryParse(environment.GetType().Name, out MetaData.Environment result) ? result : MetaData.Environment.NotAvailable;

        private static MetaData.IdentifiableInformation ConvertIdentifiableInformation(IIdentifiableInformation identifiableInformation) =>
            identifiableInformation.Identifier switch
            {
                "PersonalIdentifiableInformationPii" => MetaData.IdentifiableInformation.PersonalIdentifiableInformationPii,
                "PersonalFinanceInformationPfi" => MetaData.IdentifiableInformation.PersonalFinanceInformationPfi,
                "ProtectedHealthInformationPhi" => MetaData.IdentifiableInformation.ProtectedHealthInformationPhi,
                _ => MetaData.IdentifiableInformation.NotAvailable
            };
    }
}
