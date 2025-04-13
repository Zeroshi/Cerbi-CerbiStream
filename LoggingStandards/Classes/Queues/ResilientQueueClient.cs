using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using CerbiClientLogging.Interfaces.SendMessage;

namespace CerbiClientLogging.Classes.Queues
{
    /// <summary>
    /// A resilient queue client decorator that wraps an underlying ISendMessage implementation with Polly-based retry policies.
    /// </summary>
    public class ResilientQueueClient : ISendMessage
    {
        private readonly ISendMessage _innerQueueClient;
        private readonly AsyncRetryPolicy<bool> _retryPolicy;
        private readonly ILogger<ResilientQueueClient> _logger;

        /// <summary>
        /// Initializes a new instance of the ResilientQueueClient class.
        /// </summary>
        /// <param name="innerQueueClient">The underlying queue client to decorate.</param>
        /// <param name="logger">The logger used to log retry attempts.</param>
        public ResilientQueueClient(ISendMessage innerQueueClient, ILogger<ResilientQueueClient> logger)
        {
            _innerQueueClient = innerQueueClient;
            _logger = logger;

            _retryPolicy = Policy<bool>
                .Handle<Exception>() // Optionally, replace Exception with a more specific exception type.
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt - 1)),
                    onRetry: (exception, delay, retryCount, context) =>
                    {
                        // Retrieve the logId from the Polly execution context.
                        var logId = context.ContainsKey("LogId") ? context["LogId"] : "Unknown";
                        _logger.LogWarning("Retry attempt {RetryCount} for SendMessageAsync with LogId {LogId}. Delay: {Delay}. Exception: {ErrorMessage}",
                            retryCount, logId, delay, exception.Exception.Message);
                    });
        }

        /// <inheritdoc />
        public async Task<bool> SendMessageAsync(string payload, string messageId)
        {
            // Pass the logId within Polly's execution context.
            return await _retryPolicy.ExecuteAsync(async (context) =>
            {
                return await _innerQueueClient.SendMessageAsync(payload, messageId);
            }, new Context() { { "LogId", messageId } });
        }
    }
}
