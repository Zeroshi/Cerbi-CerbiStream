namespace CerbiStream.Enums
{
    public enum LoggingDestination
    {
        Kafka,
        RabbitMQ,
        AzureServiceBus,
        AWS_SQS,
        AWS_Kinesis,
        GooglePubSub,
        None  // ✅ Ensure 'None' exists
    }
}

