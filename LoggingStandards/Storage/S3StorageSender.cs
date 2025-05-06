using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.SQS;
using CerbiClientLogging.Interfaces.SendMessage;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CerbiStream.Storage
{
    public class S3StorageSender : ISendMessage
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3StorageSender(IAmazonS3 s3Client, string bucketName)
        {
            _s3Client = s3Client;
            _bucketName = bucketName;
        }

        public async Task<bool> SendMessageAsync(string message, string logId)
        {
            var key = $"{DateTime.UtcNow:yyyy-MM-dd/HH-mm-ss}-{logId}.json";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(message));

            var request = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = key,
                BucketName = _bucketName,
                ContentType = "application/json"
            };

            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(request);
            return true;
        }
    }
}
