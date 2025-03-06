using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CerberusLogging.Classes.Enums;
using Microsoft.Extensions.Logging;

namespace CerberusClientLogging.Interfaces
{
    public interface IBaseLogging
    {
        Task<bool> SendApplicationLogAsync(
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
            string? requestId
        );

        Task<bool> LogEventAsync(string message, LogLevel logLevel, Dictionary<string, object>? metadata = null);
        Task<bool> LogPerformanceAsync(string eventName, long elapsedMilliseconds, Dictionary<string, object>? metadata = null);
    }
}
