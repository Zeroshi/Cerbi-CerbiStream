using CerbiClientLogging.Classes;
using CerbiClientLogging.Classes.Queues;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using CerbiStream.Logging.Configuration; // ✅ This is where CerbiStreamOptions comes from
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

public class CerbiLoggerBuilder
{
    private ISendMessage? _queue;
    private bool _encryptionEnabled = true;
    private bool _debugMode = false;

    // ✅ Add a private options field
    private CerbiStreamOptions _options = new CerbiStreamOptions();

    public CerbiLoggerBuilder UseRabbitMQ(string connectionString)
    {
        // _queue = new RabbitMessageQueue(connectionString); // Uncomment if implemented
        return this;
    }

    public CerbiLoggerBuilder UseAzureServiceBus(string connectionString, string queueName)
    {
        _queue = new AzureServiceBusQueue(connectionString, queueName);
        return this;
    }

    public CerbiLoggerBuilder UseHttp(string endpoint, Dictionary<string, string>? headers = null)
    {
        _queue = new HttpMessageSender(endpoint, headers);
        return this;
    }

    public CerbiLoggerBuilder UseBlobStorage(string connectionString, string containerName)
    {
        _queue = new BlobStorageSender(connectionString, containerName);
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

    // ✅ Fluent method to configure CerbiStreamOptions externally
    public CerbiLoggerBuilder WithOptions(Action<CerbiStreamOptions> configure)
    {
        configure(_options);
        return this;
    }

    // ✅ Fixed Build method
    public Logging Build(ILogger<Logging> logger, ConvertToJson jsonConverter, IEncryption encryption)
    {
        if (_queue == null)
            throw new InvalidOperationException("A queue must be selected before building the logger.");

        return new Logging(logger, _queue, jsonConverter, encryption, _options);
    }
}
