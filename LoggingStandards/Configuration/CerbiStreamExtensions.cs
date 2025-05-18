using Cerbi.Governance;
using CerbiClientLogging.Interfaces;
using CerbiStream.Classes.FileLogging;
using CerbiStream.FileLogging;
using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace CerbiStream.Configuration
{
    public static class CerbiStreamExtensions
    {
        /// <summary>
        /// Adds CerbiStream logging to the application's logging pipeline, including optional file fallback support.
        /// </summary>
        public static ILoggingBuilder AddCerbiStream(
            this ILoggingBuilder builder,
            Action<CerbiStreamOptions> configureOptions)
        {
            var options = new CerbiStreamOptions();
            configureOptions(options);

            // Register CerbiStream options and logger provider
            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<ILoggerProvider, CerbiStreamLoggerProvider>();

            if (options.FileFallback?.Enable == true)
            {
                var fallbackConfig = options.FileFallback;
                var fallbackOptions = new CerbiStream.Classes.FileLogging.FileFallbackOptions
                {
                    Enable = fallbackConfig.Enable,
                    PrimaryFilePath = fallbackConfig.PrimaryFilePath,
                    FallbackFilePath = fallbackConfig.FallbackFilePath,
                    RetryCount = fallbackConfig.RetryCount,
                };

                // Register fallback logging components
                builder.Services.AddSingleton(fallbackOptions);
                builder.Services.AddSingleton<ILoggerProvider, FileFallbackProvider>();

                // Explicit factory registration for rotator with encryption dependency
                builder.Services.AddSingleton<EncryptedFileRotator>(sp =>
                {
                    var opts = sp.GetRequiredService<CerbiStream.Classes.FileLogging.FileFallbackOptions>();
                    var encryption = sp.GetRequiredService<IEncryption>();
                    return new EncryptedFileRotator(opts, encryption);
                });

                builder.Services.AddHostedService<EncryptedFileRotationService>();
            }

            return builder;
        }
    }
}
