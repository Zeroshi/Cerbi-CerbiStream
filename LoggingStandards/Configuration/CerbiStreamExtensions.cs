using CerbiStream.Classes.FileLogging;
using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // Explicitly use only one namespace
using System;
using CerbiStream.FileLogging;

namespace CerbiStream.Configuration
{
    public static class CerbiStreamExtensions
    {
        /// <summary>
        /// Adds CerbiStream logging to the application's logging pipeline, including optional file-fallback.
        /// </summary>
        public static ILoggingBuilder AddCerbiStream(
            this ILoggingBuilder builder,
            Action<CerbiStreamOptions> configureOptions)
        {
            // Configure core options
            var options = new CerbiStreamOptions();
            configureOptions(options);

            // Register options and primary logger provider
            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<ILoggerProvider, CerbiStreamLoggerProvider>();

            // File-fallback: map config -> runtime options, then register
            if (options.FileFallback?.Enable == true)
            {
                var cfg = options.FileFallback;
                var fbOpts = new CerbiStream.Classes.FileLogging.FileFallbackOptions
                {
                    Enable = cfg.Enable,
                    PrimaryFilePath = cfg.PrimaryFilePath,
                    FallbackFilePath = cfg.FallbackFilePath,
                    RetryCount = cfg.RetryCount,
                    RetryDelay = TimeSpan.FromMilliseconds(cfg.RetryDelayMilliseconds)
                };

                // Register fallback logger provider
                builder.Services.AddSingleton<ILoggerProvider>(new FileFallbackProvider(fbOpts));
                // Register rotation service
                builder.Services.AddSingleton(new EncryptedFileRotator(fbOpts));
                builder.Services.AddHostedService<EncryptedFileRotationService>();
                builder.AddProvider(new FileFallbackProvider(fbOpts));
            }

            return builder;
        }
    }
}
