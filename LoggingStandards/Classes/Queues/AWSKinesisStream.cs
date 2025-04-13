using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CerbiClientLogging.Classes.Queues
{
    public class AWSKinesisStream : ISendMessage
    {
        private readonly string _streamName; // Only one declaration remains.
        private readonly AmazonKinesisClient _client;

        public AWSKinesisStream(string accessKey, string secretKey, string region, string streamName)
        {
            _streamName = streamName;
            var config = new AmazonKinesisConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
            _client = new AmazonKinesisClient(accessKey, secretKey, config);
        }

        public async Task<bool> SendMessageAsync(string payload, string messageId)
        {
            try
            {
                var request = new PutRecordRequest
                {
                    StreamName = _streamName,
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(payload)),
                    PartitionKey = messageId // Using the logId passed in as a separate variable.
                };

                var response = await _client.PutRecordAsync(request);

                Console.WriteLine($"{messageId} sent to AWS Kinesis. Status: {response.HttpStatusCode}");
                return response.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send {messageId} to AWS Kinesis. Error: {ex.Message}");
                return false;
            }
        }
    }
}
