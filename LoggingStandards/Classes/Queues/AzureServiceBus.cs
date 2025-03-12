using Azure.Messaging.ServiceBus;
using CerbiClientLogging.Interfaces;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CerbiClientLogging.Classes.Queues
{
    public class AzureServiceBusQueue : IQueue, IAsyncDisposable
    {
        private readonly ServiceBusSender _sender;

        public AzureServiceBusQueue(string connectionString, string queueName)
        {
            var client = new ServiceBusClient(connectionString);
            _sender = client.CreateSender(queueName);
        }

        public async Task<bool> SendMessageAsync(string message, Guid messageId)
        {
            try
            {
                var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(message))
                {
                    ApplicationProperties = { [nameof(messageId)] = messageId.ToString() }
                };
                await _sender.SendMessageAsync(serviceBusMessage);

                Console.WriteLine($"[Azure Service Bus] {messageId} was sent.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Azure Service Bus] {messageId} was NOT sent. Error: {ex.Message}");
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _sender.DisposeAsync();
        }
    }
}
