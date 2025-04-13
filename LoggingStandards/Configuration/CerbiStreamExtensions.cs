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
        /// Adds logging to the application's logging pipeline.
        /// </summary>
        public static ILoggingBuilder AddCerbiStream(
            this ILoggingBuilder builder,
            Action<CerbiStreamOptions> configureOptions)
        {
            var options = new CerbiStreamOptions();
            configureOptions(options);

            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<ILoggerProvider, CerbiStreamLoggerProvider>();

            // Add File Fallback support if enabled
            if (options.FileFallback?.Enable == true)
            {
                builder.Services.AddSingleton<ILoggerProvider>(new FileFallbackProvider(options.FileFallback));
            }
            if (options.FileFallback?.Enable == true)
            {
                var rotator = new EncryptedFileRotator(options.FileFallback);
                builder.Services.AddSingleton(rotator);
                builder.Services.AddHostedService<EncryptedFileRotationService>();
                builder.AddProvider(new FileFallbackProvider(options.FileFallback));
            }


            return builder;
        }
    }

}


