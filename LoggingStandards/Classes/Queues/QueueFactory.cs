using CerberClientLogging.Classes.Queues;
using CerbiClientLogging.Classes.Queues;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using CerbiStream.Configuration;
using System;

namespace CerbiStream.Classes.Queues
{
    public static class QueueFactory
    {
        public static ISendMessage CreateQueue(CerbiStreamConfig config)
        {
            return config.QueueType switch
            {
                "RabbitMQ" => new RabbitMessageQueue(config.QueueConnectionString, config.QueueName),
                "Kafka" => new KafkaStream(config.QueueConnectionString, config.QueueName),
                "AzureQueue" => new AzureQueues(config.QueueConnectionString, config.QueueName),
                "AzureServiceBus" => new AzureServiceBusQueue(config.QueueConnectionString, config.QueueName),

                // 🏆 Fixed AWS SQS & Kinesis missing parameters
                "AWS_SQS" => new AwsSqsQueue(config.QueueConnectionString, config.QueueConnectionString, config.Region, config.QueueName),
                "AWS_Kinesis" => new AWSKinesisStream(config.QueueConnectionString, config.QueueConnectionString, config.Region, config.QueueName),

                "GooglePubSub" => new GooglePubSubQueue(config.QueueConnectionString, config.QueueName),

                _ => throw new NotSupportedException($"Queue type '{config.QueueType}' is not supported.")
            };
        }

    }
}

