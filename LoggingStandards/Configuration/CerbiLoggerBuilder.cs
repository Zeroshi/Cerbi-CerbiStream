using CerberClientLogging.Classes.Queues;
using CerbiClientLogging.Classes;
using CerbiClientLogging.Classes.Queues;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using CerbiStream.Logging.Configuration;
using CerbiStream.Storage;
using System;
using System.Collections.Generic;

public class CerbiLoggerBuilder
{
    private ISendMessage? _queue;
    private bool _encryptionEnabled = true;
    private bool _debugMode = false;

    private CerbiStreamOptions _options = new CerbiStreamOptions();

    public CerbiLoggerBuilder UseRabbitMQ(string host, string queueName)
    {
        _queue = new RabbitMessageQueue(host, queueName);
        _options.WithQueue("RabbitMQ", host, queueName);
        return this;
    }

    public CerbiLoggerBuilder UseAzureServiceBus(string connectionString, string queueName)
    {
        _queue = new AzureServiceBusQueue(connectionString, queueName);
        _options.WithQueue("AzureServiceBus", connectionString, queueName);
        return this;
    }

    public CerbiLoggerBuilder UseAzureQueue(string connectionString, string queueName)
    {
        _queue = new AzureQueues(connectionString, queueName);
        _options.WithQueue("AzureQueue", connectionString, queueName);
        return this;
    }

    public CerbiLoggerBuilder UseKafka(string bootstrapServers, string topic)
    {
        _queue = new KafkaStream(bootstrapServers, topic);
        _options.WithQueue("Kafka", bootstrapServers, topic);
        return this;
    }

    public CerbiLoggerBuilder UseAwsSqs(string accessKey, string secretKey, string region, string queueUrl)
    {
        _queue = new AwsSqsQueue(accessKey, secretKey, region, queueUrl);
        _options.WithQueue("AWS_SQS", region, queueUrl);
        return this;
    }

    public CerbiLoggerBuilder UseAwsKinesis(string accessKey, string secretKey, string region, string streamName)
    {
        _queue = new AWSKinesisStream(accessKey, secretKey, region, streamName);
        _options.WithQueue("AWS_Kinesis", region, streamName);
        return this;
    }

    public CerbiLoggerBuilder UseGooglePubSub(string projectId, string topicId)
    {
        _queue = new GooglePubSubQueue(projectId, topicId);
        _options.WithQueue("GooglePubSub", projectId, topicId);
        return this;
    }

    public CerbiLoggerBuilder UseHttp(string endpoint, Dictionary<string, string>? headers = null)
    {
        _queue = new HttpMessageSender(endpoint, headers);
        _options.WithQueue("HttpEndpoint", endpoint, "HttpMessage");
        return this;
    }

    public CerbiLoggerBuilder UseBlobStorage(string connectionString, string containerName)
    {
        _queue = new BlobStorageSender(connectionString, containerName);
        _options.WithQueue("AzureBlob", connectionString, containerName);
        return this;
    }

    public CerbiLoggerBuilder UseS3(Amazon.S3.IAmazonS3 s3Client, string bucketName)
    {
        _queue = new S3StorageSender(s3Client, bucketName);
        _options.WithQueue("AWS_S3", "IAM", bucketName);
        return this;
    }

    public CerbiLoggerBuilder UseGcs(Google.Cloud.Storage.V1.StorageClient gcsClient, string bucketName)
    {
        _queue = new GcsStorageSender(gcsClient, bucketName);
        _options.WithQueue("GCP_GCS", "IAM", bucketName);
        return this;
    }

    // ✅ Platform Presets

    public CerbiLoggerBuilder UseAzurePlatform(string queueConnectionString, string queueName, string blobConnectionString, string blobContainer, bool useQueue = true, bool useBlob = true)
    {
        if (useQueue)
        {
            UseAzureServiceBus(queueConnectionString, queueName);
        }
        if (useBlob)
        {
            UseBlobStorage(blobConnectionString, blobContainer);
        }
        return this;
    }

    public CerbiLoggerBuilder UseAwsPlatform(string accessKey, string secretKey, string region, string queueUrl, Amazon.S3.IAmazonS3 s3Client, string bucketName, bool useQueue = true, bool useS3 = true)
    {
        if (useQueue)
        {
            UseAwsSqs(accessKey, secretKey, region, queueUrl);
        }
        if (useS3)
        {
            UseS3(s3Client, bucketName);
        }
        return this;
    }

    public CerbiLoggerBuilder UseGcpPlatform(string projectId, string topicId, Google.Cloud.Storage.V1.StorageClient gcsClient, string bucketName, bool useQueue = true, bool useGcs = true)
    {
        if (useQueue)
        {
            UseGooglePubSub(projectId, topicId);
        }
        if (useGcs)
        {
            UseGcs(gcsClient, bucketName);
        }
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

    public CerbiLoggerBuilder WithOptions(Action<CerbiStreamOptions> configure)
    {
        configure(_options);
        return this;
    }

    public Logging Build(ConvertToJson jsonConverter, IEncryption encryption)
    {
        if (_queue == null)
            throw new InvalidOperationException("A queue must be selected before building the logger.");
        return new Logging(_queue, jsonConverter, encryption, _options);
    }

}
