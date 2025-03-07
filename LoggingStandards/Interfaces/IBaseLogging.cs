using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CerberusLogging.Classes.Enums;
using Microsoft.Extensions.Logging;

namespace CerbiClientLogging.Interfaces
{
    public interface IBaseLogging
    {
        /// <summary>
        /// Logs a general application event with optional metadata.
        /// </summary>
        Task<bool> LogEventAsync(string message, LogLevel logLevel, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Logs a performance-related event with execution time and optional metadata.
        /// </summary>
        Task<bool> LogPerformanceAsync(string eventName, long elapsedMilliseconds, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Logs structured application data with enriched metadata.
        /// </summary>
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
           string? cloudProvider,       // ✅ MUST MATCH Logging.cs
           string? instanceId,
           string? applicationVersion,
           string? region,
           string? requestId
       );

    }
}
