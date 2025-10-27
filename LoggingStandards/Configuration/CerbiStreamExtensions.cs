using Cerbi.Governance;
using CerbiClientLogging.Interfaces;
using CerbiStream.Classes.FileLogging;
using CerbiStream.FileLogging;
using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using CerbiStream.Observability;
using Microsoft.AspNetCore.Builder;
using CerbiStream.Encryption; // EncryptionFactory
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

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

            // Enable async console if requested
            if (options.EnableAsyncConsoleOutput)
            {
                CerbiStream.Extensions.CerbiLoggerWrapper.EnableAsyncConsole();
            }

            // Register CerbiStream options and logger provider
            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<ILoggerProvider, CerbiStreamLoggerProvider>();

            // Register governance runtime validator
            builder.Services.AddSingleton<RuntimeGovernanceValidator>(sp =>
            {
                var settings = new RuntimeGovernanceSettings(); // Load from config if needed
                var source = new FileGovernanceSource(settings.ConfigPath);
                return new RuntimeGovernanceValidator(
                    isEnabled: () => settings.Enabled,
                    profileName: settings.Profile,
                    source: source
                );
            });

            // Register encryption service based on options (used by file rotation and other features)
            builder.Services.AddSingleton<IEncryption>(sp => EncryptionFactory.GetEncryption(options));

            if (options.FileFallback?.Enable == true)
            {
                var fallbackConfig = options.FileFallback;
                var fallbackOptions = new CerbiStream.Classes.FileLogging.FileFallbackOptions
                {
                    Enable = fallbackConfig.Enable,
                    PrimaryFilePath = fallbackConfig.PrimaryFilePath,
                    FallbackFilePath = fallbackConfig.FallbackFilePath,
                    RetryCount = fallbackConfig.RetryCount,
                    RetryDelay = fallbackConfig.RetryDelay,
                    MaxFileSizeBytes = fallbackConfig.MaxFileSizeBytes,
                    MaxFileAge = fallbackConfig.MaxFileAge,
                    EncryptionKey = fallbackConfig.EncryptionKey,
                    EncryptionIV = fallbackConfig.EncryptionIV
                };

                builder.Services.AddSingleton(fallbackOptions);
                builder.Services.AddSingleton<ILoggerProvider, FileFallbackProvider>();

                // Only register encrypted rotation if encryption is enabled
                if (options.EncryptionMode != EncryptionType.None)
                {
                    builder.Services.AddSingleton<EncryptedFileRotator>(sp =>
                    {
                        var opts = sp.GetRequiredService<CerbiStream.Classes.FileLogging.FileFallbackOptions>();
                        var encryption = sp.GetRequiredService<IEncryption>();
                        return new EncryptedFileRotator(opts, encryption);
                    });

                    builder.Services.AddHostedService<EncryptedFileRotationService>();
                }
            }

            // HealthHostedService
            builder.Services.AddHostedService<HealthHostedService>(sp => new HealthHostedService(sp.GetRequiredService<ILogger<HealthHostedService>>()));

            // Wire telemetry provider into Metrics if present
            if (options.TelemetryProvider != null)
            {
                Metrics.TelemetryProvider = options.TelemetryProvider;
            }

            return builder;
        }

        /// <summary>
        /// Convenience overload that binds options from IConfiguration using the typical pattern.
        /// </summary>
        public static ILoggingBuilder AddCerbiStream(this ILoggingBuilder builder)
        {
            var options = new CerbiStreamOptions();
            builder.Services.AddSingleton(options);

            // Register default runtime components
            builder.Services.AddSingleton<ILoggerProvider, CerbiStreamLoggerProvider>();
            builder.Services.AddSingleton<RuntimeGovernanceValidator>(sp =>
            {
                var settings = new RuntimeGovernanceSettings();
                var source = new FileGovernanceSource(settings.ConfigPath);
                return new RuntimeGovernanceValidator(
                    isEnabled: () => settings.Enabled,
                    profileName: settings.Profile,
                    source: source
                );
            });

            // Register encryption from options (defaults to NoOp)
            builder.Services.AddSingleton<IEncryption>(sp => EncryptionFactory.GetEncryption(options));

            builder.Services.AddHostedService<HealthHostedService>(sp => new HealthHostedService(sp.GetRequiredService<ILogger<HealthHostedService>>()));

            if (options.TelemetryProvider != null)
            {
                Metrics.TelemetryProvider = options.TelemetryProvider;
            }

            return builder;
        }

        /// <summary>
        /// Registers a lightweight health and metrics endpoint integration for ASP.NET Core.
        /// </summary>
        public static ILoggingBuilder AddCerbiStreamHealthChecks(this ILoggingBuilder builder)
        {
            builder.Services.AddHealthChecks();
            builder.Services.AddSingleton<CerbiStream.Middleware.CerbiMetricsMiddleware>();
            return builder;
        }

        public static IApplicationBuilder UseCerbiStreamMetrics(this IApplicationBuilder app)
        {
            app.UseMiddleware<CerbiStream.Middleware.CerbiMetricsMiddleware>();
            return app;
        }
    }
}
