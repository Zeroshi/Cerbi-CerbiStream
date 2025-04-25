using Azure.Storage.Blobs;
using CerbiClientLogging.Interfaces.SendMessage;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class BlobStorageSender : ISendMessage
{
    private readonly BlobContainerClient _container;

    public BlobStorageSender(string connectionString, string containerName)
    {
        _container = new BlobContainerClient(connectionString, containerName);
        _container.CreateIfNotExists();
    }

    public async Task<bool> SendMessageAsync(string message, string logId)
    {
        var blobName = $"{DateTime.UtcNow:yyyy-MM-dd/HH-mm-ss}-{logId}.json";
        var blob = _container.GetBlobClient(blobName);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(message));
        await blob.UploadAsync(stream, overwrite: true);
        return true;
    }
}
