using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CerbiStream.Classes.FileLogging
{
    public class EncryptedFileRotationService : BackgroundService
    {
        private readonly EncryptedFileRotator _rotator;
        private readonly ILogger<EncryptedFileRotationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

        public EncryptedFileRotationService(EncryptedFileRotator rotator, ILogger<EncryptedFileRotationService> logger)
        {
            _rotator = rotator;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cerbi file rotation service started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _rotator.CheckAndRotateIfNeeded();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Encrypted log rotation failed.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
