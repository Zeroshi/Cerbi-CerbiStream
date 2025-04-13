using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CerberClientLogging.Classes.Queues
{
    /// <summary>
    /// Represents a RabbitMQ queue for sending messages.
    /// </summary>
    public class RabbitMessageQueue : ISendMessage
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
        public async Task<bool> SendMessageAsync(string message, string messageId)
        {
            try
            {
                using var connection = await _factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var body = Encoding.UTF8.GetBytes(message ?? string.Empty);

                // Fix for CS0128: Renamed the variable to avoid duplicate declaration
                // Fix for CS1061: Replaced the incorrect method call with a valid approach
                var basicProperties = new BasicProperties
                {
                    Persistent = true
                };

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: _queueName,
                    mandatory: false,
                    basicProperties: basicProperties,
                    body: body
                );

                Console.WriteLine($"[RabbitMQ] {messageId} was sent.");
                return true;
            }
            catch (BrokerUnreachableException ex)
            {
                Console.WriteLine($"[RabbitMQ] {messageId} was NOT sent. Broker unreachable: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQ] {messageId} was NOT sent. Error: {ex.Message}");
                return false;
            }
        }
    }
}
