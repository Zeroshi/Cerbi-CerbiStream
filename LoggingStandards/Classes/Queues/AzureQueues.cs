using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;

namespace CerbiClientLogging.Classes.Queues
{
    public class AzureQueueStorage : ISendMessage, IQueue
    {
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly QueueClient _client;

        public AzureQueueStorage(string connectionString, string queueName)
        {
            _connectionString = connectionString;
            _queueName = queueName;

            // Create a QueueClient instance for the specified queue
            _client = new QueueClient(_connectionString, _queueName);

            // Create the queue if it does not exist
            _client.CreateIfNotExists();
        }

        public async Task<bool> SendMessageAsync(string payload, Guid messageId)
        {
            try
            {
                // Add the message to the queue
                await _client.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)));
                return true;

                Console.WriteLine(messageId + @" was sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"{messageId} was not sent. Error: {ex.InnerException}");
                return false;
            }
        }
    }
}