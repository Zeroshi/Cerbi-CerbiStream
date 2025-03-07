//using System;
//using System.Text;
//using System.Threading.Tasks;
//using RabbitMQ.Client;

//namespace CerbiClientLogging.Classes.Queues
//{
//    public class RabbitMqQueue : IDisposable
//    {
//        private readonly string _queueName;
//        private readonly IConnection _connection;
//        private readonly IModel _channel;

//        public RabbitMqQueue(string connectionString, string queueName)
//        {
//            _queueName = queueName;

//            // Create RabbitMQ connection
//            var factory = new ConnectionFactory
//            {
//                Uri = new Uri(connectionString)
//            };

//            // Open connection and create a channel
//            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult(); // Fix: Await the task result synchronously
//            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult(); // Fix: Await the task result synchronously

//            // Declare the queue
//            _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
//        }

//        public async Task<bool> SendMessageAsync(string message, Guid messageId)
//        {
//            try
//            {
//                var body = Encoding.UTF8.GetBytes(message);

//                await Task.Run(() =>
//                    _channel.BasicPublish(exchange: "", routingKey: _queueName, basicProperties: null, body: body)
//                );

//                Console.WriteLine($"{messageId} was sent.");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"{messageId} was not sent. Error: {ex.Message}");
//                return false;
//            }
//        }

//        public void Dispose()
//        {
//            // Dispose of the channel and connection properly
//            _channel?.Dispose();
//            _connection?.Dispose();
//        }
//    }
//}
