using System;
using System.Threading.Tasks;
using CerbiClientLogging.Interfaces;
using CerbiStream.Configuration;
using CerbiStream.Enums;
using Microsoft.Extensions.Logging;

namespace CerbiClientLogging.Classes.Queues
{
    public class SendToQueue
    {
        private readonly ITransactionDestination _transactionDestination;
        private readonly CerbiStreamConfig _config;
        private readonly ILogger<SendToQueue> _logger;

        public SendToQueue(
            ITransactionDestination transactionDestination,
            CerbiStreamConfig config,
            ILogger<SendToQueue> logger)
        {
            _transactionDestination = transactionDestination ?? throw new ArgumentNullException(nameof(transactionDestination));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SendLogAsync(string formattedLog)
        {
            try
            {
                LoggingDestination destination = _config.Destination;

                switch (destination)
                {
                    case LoggingDestination.Kafka:
                        await _transactionDestination.SendLogAsync(formattedLog, TransactionDestinationTypes.Kafka);
                        _logger.LogInformation("[Kafka] Log Sent.");
                        break;

                    case LoggingDestination.RabbitMQ:
                        await _transactionDestination.SendLogAsync(formattedLog, TransactionDestinationTypes.RabbitMQ);
                        _logger.LogInformation("[RabbitMQ] Log Sent.");
                        break;

                    case LoggingDestination.AzureServiceBus:
                        await _transactionDestination.SendLogAsync(formattedLog, TransactionDestinationTypes.AzureServiceBus);
                        _logger.LogInformation("[Azure Service Bus] Log Sent.");
                        break;

                    case LoggingDestination.AWS_SQS:
                        await _transactionDestination.SendLogAsync(formattedLog, TransactionDestinationTypes.AWS_SQS);
                        _logger.LogInformation("[AWS SQS] Log Sent.");
                        break;

                    case LoggingDestination.AWS_Kinesis:
                        await _transactionDestination.SendLogAsync(formattedLog, TransactionDestinationTypes.AWS_Kinesis);
                        _logger.LogInformation("[AWS Kinesis] Log Sent.");
                        break;

                    case LoggingDestination.GooglePubSub:
                        await _transactionDestination.SendLogAsync(formattedLog, TransactionDestinationTypes.GooglePubSub);
                        _logger.LogInformation("[Google Pub/Sub] Log Sent.");
                        break;

                    case LoggingDestination.None:
                        _logger.LogWarning("Logging disabled. No logs sent.");
                        return false;

                    default:
                        _logger.LogWarning($"Unsupported logging destination: {destination}");
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send log to the queue.");
                return false;
            }
        }
    }
}
