using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using CerbiClientLogging.Interfaces.SendMessage;

namespace CerbiClientLogging.Classes.Queues
{
    public class AwsSqsQueue : ISendMessage
    {
        private readonly AmazonSQSClient _sqsClient;
        private readonly string _queueUrl;

        public AwsSqsQueue(string accessKey, string secretKey, string region, string queueUrl)
        {
            _sqsClient = new AmazonSQSClient(accessKey, secretKey, Amazon.RegionEndpoint.GetBySystemName(region));
            _queueUrl = queueUrl;
        }

        public async Task<bool> SendMessageAsync(string message, Guid messageId)
        {
            try
            {
                var sendMessageRequest = new SendMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MessageBody = message
                };

                await _sqsClient.SendMessageAsync(sendMessageRequest);
                Console.WriteLine($"[AWS SQS] {messageId} was sent.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AWS SQS] {messageId} was NOT sent. Error: {ex.Message}");
                return false;
            }
        }
    }
}
