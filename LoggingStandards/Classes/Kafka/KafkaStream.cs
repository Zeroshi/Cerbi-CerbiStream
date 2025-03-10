using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CerbiClientLogging.Interfaces.SendMessage;

namespace CerbiClientLogging.Classes.Queues
{
    public class KafkaStream : ISendMessage
    {
        private readonly string _bootstrapServers;
        private readonly string _topic;

        public KafkaStream(string bootstrapServers, string topic)
        {
            _bootstrapServers = bootstrapServers;
            _topic = topic;
        }

        public async Task<bool> SendMessageAsync(string payload, Guid messageId)
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
