using CerbiStream.Configuration;
using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // Explicitly use only one namespace
using System;

namespace CerbiStream.Logging.Extensions
{
    public static class CerbiStreamExtensions
    {
        /// <summary>
        /// Adds CerbiStream logging to the application's logging pipeline.
        /// </summary>
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddCerbiStream(
            this Microsoft.Extensions.Logging.ILoggingBuilder builder,
            Action<CerbiStreamOptions> configureOptions)
        {
            var options = new CerbiStreamOptions();
            configureOptions(options);

            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<ILoggerProvider, CerbiStreamLoggerProvider>();

            return builder;
        }
    }
}


