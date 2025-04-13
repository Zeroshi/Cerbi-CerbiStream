using Azure.Storage.Queues;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CerbiClientLogging.Classes.Queues
{
    public class AzureQueues : ISendMessage
    {
        private readonly QueueClient _client;

        public AzureQueues(string connectionString, string queueName)
        {
            _client = new QueueClient(connectionString, queueName);
            _client.CreateIfNotExists();
        }

        public async Task<bool> SendMessageAsync(string payload, string messageId)
        {
            try
            {
                await _client.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)));
                Console.WriteLine($"[Azure Queue] {messageId} was sent.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Azure Queue] {messageId} was NOT sent. Error: {ex.Message}");
                return false;
            }
        }
    }
}
