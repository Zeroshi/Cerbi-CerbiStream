using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using CerbiClientLogging.Interfaces.SendMessage;

namespace CerbiClientLogging.Classes.Queues
{
    public class AzureServiceBusQueue : ISendMessage, IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;

        public AzureServiceBusQueue(string connectionString, string queueName)
        {
            _connectionString = connectionString;
            _queueName = queueName;

            // Create a Service Bus client
            _client = new ServiceBusClient(_connectionString);

            // Create a Service Bus sender for the specified queue
            _sender = _client.CreateSender(_queueName);
        }

        public async Task<bool> SendMessageAsync(string message, Guid messageId)
        {
            try
            {
                // Create a Service Bus message from the provided string message
                var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(message));

                // Add a custom message property for the provided message ID
                serviceBusMessage.ApplicationProperties.Add(nameof(messageId), messageId.ToString());

                // Send the message to the queue
                await _sender.SendMessageAsync(serviceBusMessage);

                Console.WriteLine(messageId + " was sent");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"{messageId} was not sent. Error: {ex}");
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Dispose of the Service Bus sender and client
            await _sender.DisposeAsync();
            await _client.DisposeAsync();
        }
    }
}