using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CerbiClientLogging.Classes.Queues
{
    public class GooglePubSubQueue : ISendMessage, IQueue
    {
        private readonly string _projectId;
        private readonly string _topicId;
        private readonly PublisherClient _publisher;

        public GooglePubSubQueue(string projectId, string topicId)
        {
            _projectId = projectId;
            _topicId = topicId;
            _publisher = PublisherClient.CreateAsync(new TopicName(_projectId, _topicId)).Result;
        }

        public async Task<bool> SendMessageAsync(string message, Guid messageId)
        {
            try
            {
                TopicName topicName = new TopicName(_projectId, _topicId);
                PubsubMessage pubsubMessage = new PubsubMessage
                {
                    Data = ByteString.CopyFromUtf8(message),
                    Attributes = { { "messageId", messageId.ToString() } }
                };

                var messageIdResponse = await _publisher.PublishAsync(pubsubMessage);

                Console.WriteLine($"{messageId} sent to Google Pub/Sub. Message ID: {messageIdResponse}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send {messageId} to Google Pub/Sub. Error: {ex.Message}");
                return false;
            }
        }
    }
}
