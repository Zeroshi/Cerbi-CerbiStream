using CerbiClientLogging.Interfaces.SendMessage;
using Google.Cloud.Storage.V1;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CerbiStream.Storage
{
    public class GcsStorageSender : ISendMessage
    {
        private readonly StorageClient _client;
        private readonly string _bucketName;

        public GcsStorageSender(StorageClient client, string bucketName)
        {
            _client = client;
            _bucketName = bucketName;
        }

        public async Task<bool> SendMessageAsync(string message, string logId)
        {
            var objectName = $"{DateTime.UtcNow:yyyy-MM-dd/HH-mm-ss}-{logId}.json";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(message));
            await _client.UploadObjectAsync(_bucketName, objectName, "application/json", stream);
            return true;
        }
    }
}
