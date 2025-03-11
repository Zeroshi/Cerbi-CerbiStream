using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

