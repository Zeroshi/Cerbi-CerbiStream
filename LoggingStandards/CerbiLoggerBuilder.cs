using CerbiClientLogging.Classes;
using CerbiClientLogging.Classes.Queues;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using Microsoft.Extensions.Logging;
using System;

public class CerbiLoggerBuilder
{
    private IQueue? _queue;
    private bool _encryptionEnabled = true;
    private bool _debugMode = false;

    public CerbiLoggerBuilder UseRabbitMQ(string connectionString)
    {
        //_queue = new RabbitMqQueue(connectionString);
        return this;
    }

    public CerbiLoggerBuilder UseAzureServiceBus(string connectionString, string queueName)
    {
        _queue = new AzureServiceBusQueue(connectionString, queueName);
        return this;
    }

    public CerbiLoggerBuilder EnableEncryption(bool enable = true)
    {
        _encryptionEnabled = enable;
        return this;
    }

    public CerbiLoggerBuilder EnableDebugMode(bool enable = true)
    {
        _debugMode = enable;
        return this;
    }

    public Logging Build(ILogger<Logging> logger, ConvertToJson jsonConverter, IEncryption encryption)
    {
        if (_queue == null)
            throw new InvalidOperationException("A queue must be selected before building the logger.");

        return new Logging(logger, _queue, jsonConverter, encryption);
    }
}
