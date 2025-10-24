using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CerbiStream.Classes.FileLogging
{
    /// <summary>
    /// The EncryptedFileRotationService class is a hosted background service designed to monitor
    /// and perform automated encrypted file rotation tasks at periodic intervals.
    /// </summary>
    /// <remarks>
    /// This service integrates with the application's dependency injection system and executes
    /// encrypted file rotation processes using an injected EncryptedFileRotator instance.
    /// Errors encountered during rotation are logged using the provided ILogger instance.
    /// </remarks>
    /// <example>
    /// To enable this service, ensure that a proper configuration for file rotation is
    /// registered via CerbiStream extensions, including options like file paths and encryption setup.
    /// </example>
    /// <seealso cref="Microsoft.Extensions.Hosting.BackgroundService" />
    public class EncryptedFileRotationService : BackgroundService
    {
        /// <summary>
        /// The `_rotator` variable is an instance of the <see cref="EncryptedFileRotator"/> class,
        /// responsible for managing the rotation of encrypted files based on size or age thresholds.
        /// It performs checks to determine whether the current log file needs to be rotated, ensuring
        /// that log files do not exceed specified limits and are securely archived.
        /// </summary>
        private readonly EncryptedFileRotator _rotator;

        /// <summary>
        /// Provides logging capabilities for the <see cref="EncryptedFileRotationService"/>.
        /// Serves as an instance of <see cref="ILogger"/> to log informational, warning, and error messages
        /// related to file rotation and other operations carried out by the service.
        /// </summary>
        private readonly ILogger<EncryptedFileRotationService> _logger;

        /// <summary>
        /// Represents the interval at which the service checks and triggers the encrypted file rotation process.
        /// </summary>
        /// <remarks>
        /// This value defines the time delay between subsequent checks for log file rotation conditions,
        /// such as file size or age. It ensures that the rotation process is periodically executed to
        /// manage the log file within defined constraints.
        /// </remarks>
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

        /// Represents a background service responsible for encrypted file rotation.
        /// Utilizes an instance of EncryptedFileRotator to perform log file rotation
        /// based on size or age thresholds, ensuring encrypted storage of archived logs.
        public EncryptedFileRotationService(EncryptedFileRotator rotator, ILogger<EncryptedFileRotationService> logger)
        {
            _rotator = rotator;
            _logger = logger;
        }

        /// <summary>
        /// Executes the main background logic for the EncryptedFileRotationService.
        /// Monitors and triggers log file rotation at regular intervals or based on defined conditions,
        /// ensuring encrypted log files are properly maintained.
        /// </summary>
        /// <param name="stoppingToken">A cancellation token that signals when the background service should stop processing.</param>
        /// <returns>A task that represents the ongoing operation of the service.</returns>
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
