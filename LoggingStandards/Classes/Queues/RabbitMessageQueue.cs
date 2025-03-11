using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CerberClientLogging.Classes.Queues
{
    /// <summary>
    /// Represents a RabbitMQ queue for sending messages.
    /// </summary>
    public class RabbitMessageQueue : ISendMessage, IQueue
    {
        private readonly string _hostName;
        private readonly string _queueName;
        private readonly ConnectionFactory _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMessageQueue"/> class.
        /// </summary>
        public RabbitMessageQueue(string hostName, string queueName)
        {
            _hostName = hostName;
            _queueName = queueName;

            _factory = new ConnectionFactory()
            {
                HostName = _hostName
            };
        }

        /// <summary>
        /// Sends a message asynchronously to the RabbitMQ queue.
        /// </summary>
        public Task<bool> SendMessageAsync(string message, Guid messageId)
        {
            try
            {
                using var connection = _factory.CreateConnection(new string[] { _hostName });
                using var channel = connection.CreateModel();

                // Declare queue to ensure it exists
                channel.QueueDeclare(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var body = Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true; // Ensures message durability

                // Publish the message
                channel.BasicPublish(
                    exchange: "",
                    routingKey: _queueName,
                    basicProperties: properties,
                    body: body
                );

                Console.WriteLine($"[RabbitMQ] {messageId} was sent.");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQ] {messageId} was NOT sent. Error: {ex.Message}");
                return Task.FromResult(false);
            }
        }
    }
}
