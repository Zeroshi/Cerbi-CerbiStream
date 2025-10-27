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
using CerbiStream.GovernanceRuntime.Governance;

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

            // If governance is enabled, register GovernanceLoggerProvider; else dev adapter
            if (options.EnableGovernanceChecks)
            {
                builder.Services.AddSingleton<ILoggerProvider>(sp =>
                {
                    // Expect a policy path via env or default lookup inside adapter
                    var innerFactory = sp.GetRequiredService<ILoggerFactory>();
                    var adapter = new GovernanceRuntimeAdapter(profileName: "default", configPath: Environment.GetEnvironmentVariable("CERBI_GOVERNANCE_PATH"));
                    return new GovernanceLoggerProvider(innerFactory, adapter);
                });
            }
            else
            {
                builder.Services.AddSingleton<ILoggerProvider, CerbiStreamLoggerProvider>();
            }

            // Governance runtime validator (available to consumers)
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

            // Encryption service
            builder.Services.AddSingleton<IEncryption>(sp => EncryptionFactory.GetEncryption(options));

            // File fallback registration
            if (options.FileFallback?.Enable == true)
            {
                var f = options.FileFallback;
                var fallbackOptions = new CerbiStream.Classes.FileLogging.FileFallbackOptions
                {
                    Enable = f.Enable,
                    PrimaryFilePath = f.PrimaryFilePath,
                    FallbackFilePath = f.FallbackFilePath,
                    RetryCount = f.RetryCount,
                    RetryDelay = f.RetryDelay,
                    MaxFileSizeBytes = f.MaxFileSizeBytes,
                    MaxFileAge = f.MaxFileAge,
                    EncryptionKey = f.EncryptionKey,
                    EncryptionIV = f.EncryptionIV
                };
                builder.Services.AddSingleton(fallbackOptions);
                builder.Services.AddSingleton<ILoggerProvider, FileFallbackProvider>();

                if (options.EncryptionMode != EncryptionType.None)
                {
                    builder.Services.AddSingleton<EncryptedFileRotator>(sp =>
                    {
                        var opts = sp.GetRequiredService<CerbiStream.Classes.FileLogging.FileFallbackOptions>();
                        var enc = sp.GetRequiredService<IEncryption>();
                        return new EncryptedFileRotator(opts, enc);
                    });
                    builder.Services.AddHostedService<EncryptedFileRotationService>();
                }
            }

            builder.Services.AddHostedService<HealthHostedService>(sp => new HealthHostedService(sp.GetRequiredService<ILogger<HealthHostedService>>()));

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

            // Default: dev adapter; governance is opt-in via options overload
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
