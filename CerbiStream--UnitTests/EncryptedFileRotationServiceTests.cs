using CerbiStream.Classes.FileLogging;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CerbiStream.Tests
{


    public class EncryptedFileRotationServiceTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldCallRotatorPeriodically()
        {
            var tempFallback = Path.GetTempFileName();
            var options = new FileFallbackOptions
            {
                Enable = true,
                FallbackFilePath = tempFallback,
                MaxFileSizeBytes = 1, // Trigger rotation
                MaxFileAge = TimeSpan.FromMilliseconds(1),
                EncryptionKey = "12345678901234567890123456789012",
                EncryptionIV = "1234567890123456"
            };

            File.WriteAllText(tempFallback, "trigger rotation");

            var rotator = new EncryptedFileRotator(options);
            var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<EncryptedFileRotationService>();
            var service = new EncryptedFileRotationService(rotator, logger);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await service.StartAsync(cts.Token);
            await Task.Delay(500);
            await service.StopAsync(cts.Token);

            var rotatedFiles = Directory.GetFiles(Path.GetDirectoryName(tempFallback)!, "*.enc");
            Assert.NotEmpty(rotatedFiles);
        }

    }
}