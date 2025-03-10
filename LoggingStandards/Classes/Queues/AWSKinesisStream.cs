using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using CerbiClientLogging.Interfaces.SendMessage;

namespace CerbiClientLogging.Classes.Queues
{
    public class AWSKinesisStream : ISendMessage
    {
        private readonly string _streamName;
        private readonly AmazonKinesisClient _client;

        public AWSKinesisStream(string accessKey, string secretKey, string region, string streamName)
        {
            _streamName = streamName;
            var config = new AmazonKinesisConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
            _client = new AmazonKinesisClient(accessKey, secretKey, config);
        }

        public async Task<bool> SendMessageAsync(string message, Guid messageId)
        {
            try
            {
                var request = new PutRecordRequest
                {
                    StreamName = _streamName,
                    Data = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(message)),
                    PartitionKey = messageId.ToString()
                };

                var response = await _client.PutRecordAsync(request);

                Console.WriteLine($"{messageId} sent to AWS Kinesis. Status: {response.HttpStatusCode}");
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send {messageId} to AWS Kinesis. Error: {ex.Message}");
                return false;
            }
        }
    }
}
