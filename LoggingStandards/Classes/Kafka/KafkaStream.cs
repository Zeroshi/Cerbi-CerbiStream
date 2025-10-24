using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CerbiClientLogging.Classes.Queues
{
    /// <summary>
    /// Represents a Kafka stream that allows sending messages to a specified Kafka instance and topic.
    /// </summary>
    public class KafkaStream : ISendMessage
    {
        /// <summary>
        /// Represents the bootstrap server configuration string for the Kafka broker connection.
        /// This is the address of the Kafka broker(s) that the client will connect to for producing messages.
        /// </summary>
        private readonly string _bootstrapServers;

        /// <summary>
        /// Represents the Kafka topic to which messages will be sent.
        /// </summary>
        private readonly string _topic;

        /// <summary>
        /// Represents a Kafka message stream for sending messages to a Kafka topic.
        /// Implements the <see cref="ISendMessage"/> interface to provide functionality for sending payloads
        /// with associated message identifiers to Kafka.
        /// </summary>
        public KafkaStream(string bootstrapServers, string topic)
        {
            _bootstrapServers = bootstrapServers;
            _topic = topic;
        }

        /// <summary>
        /// Sends a message asynchronously to the configured Kafka stream.
        /// </summary>
        /// <param name="payload">The message content to be sent to Kafka.</param>
        /// <param name="messageId">A unique identifier for the message, used for tracking and logging purposes.</param>
        /// <returns>A task representing the asynchronous operation. The task result is a boolean indicating whether the message was successfully sent.</returns>
        public async Task<bool> SendMessageAsync(string payload, string messageId)
        {
            try
            {
                string kafkaCommand = $"echo '{payload}' | kafka-console-producer --topic {_topic} --broker-list {_bootstrapServers}";
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/C {kafkaCommand}";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    await process.WaitForExitAsync();
                }

                Console.WriteLine($"[Kafka] {messageId} was sent.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Kafka] {messageId} was NOT sent. Error: {ex.Message}");
                return false;
            }
        }
    }
}
