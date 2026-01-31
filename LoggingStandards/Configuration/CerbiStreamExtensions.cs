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
    /// <summary>
    /// Extension methods for adding CerbiStream to your application.
    /// </summary>
    /// <example>
    /// Minimal setup (zero-config, just works):
    /// <code>
    /// builder.Logging.AddCerbiStream();
    /// </code>
    /// 
    /// With governance enabled:
    /// <code>
    /// builder.Logging.AddCerbiStream(options => options.EnableDeveloperMode());
    /// </code>
    /// 
    /// Production setup:
    /// <code>
    /// builder.Logging.AddCerbiStream(options => options
    ///     .ForProduction()
    ///     .WithGovernanceProfile("myapp"));
    /// </code>
    /// </example>
    public static class CerbiStreamExtensions
    {
        private const string DefaultGovernanceFileName = "cerbi_governance.json";
        private const string DefaultGovernanceContent = @"{
  ""Version"": ""1.0"",
  ""LoggingProfiles"": {
    ""default"": {
      ""DisallowedFields"": [""password"", ""ssn"", ""creditCard"", ""secret"", ""token"", ""apiKey""],
      ""FieldSeverities"": {}
    }
  }
}";

        /// <summary>
        /// Adds CerbiStream logging with zero configuration. Just works out of the box.
        /// Auto-detects environment variables if set, otherwise uses developer mode defaults.
        /// Creates a default governance policy if none exists.
        /// </summary>
        /// <example>
        /// <code>
        /// // That's it! One line to add secure, governed logging:
        /// builder.Logging.AddCerbiStream();
        /// 
        /// // Behavior controlled by environment variables:
        /// // CERBISTREAM_MODE=production → production settings
        /// // CERBISTREAM_MODE=development → development settings (default)
        /// </code>
        /// </example>
        public static ILoggingBuilder AddCerbiStream(this ILoggingBuilder builder)
        {
            // Auto-detect: if environment variables are set, use them; otherwise use developer mode
            if (CerbiStreamOptions.ShouldUseEnvironmentConfig())
            {
                return builder.AddCerbiStream(options => options.FromEnvironment());
            }
            return builder.AddCerbiStream(options => options.EnableDeveloperMode());
        }

        /// <summary>
        /// Adds CerbiStream logging configured entirely from environment variables.
        /// Use this when you want explicit environment-based configuration.
        /// </summary>
        /// <example>
        /// <code>
        /// // Explicit environment configuration
        /// builder.Logging.AddCerbiStreamFromEnvironment();
        /// </code>
        /// </example>
        public static ILoggingBuilder AddCerbiStreamFromEnvironment(this ILoggingBuilder builder)
        {
            return builder.AddCerbiStream(options => options.FromEnvironment());
        }

        /// <summary>
        /// Adds CerbiStream logging with custom configuration.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="configureOptions">Action to configure CerbiStream options.</param>
        /// <example>
        /// <code>
        /// builder.Logging.AddCerbiStream(options => options
        ///     .ForProduction()
        ///     .WithGovernanceProfile("myservice")
        ///     .WithQueueRetries(true, retryCount: 3));
        /// </code>
        /// </example>
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
                    // Use caller-provided inner factory when available; else minimal no-op factory (dev fallback)
                    var innerFactory = options.InnerFactoryProvider?.Invoke() ?? LoggerFactory.Create(b => { });
                    var profile = string.IsNullOrWhiteSpace(options.GovernanceProfileName) ? "default" : options.GovernanceProfileName;
                    var path = options.GovernanceConfigPath ?? Environment.GetEnvironmentVariable("CERBI_GOVERNANCE_PATH");
                    var adapter = new GovernanceRuntimeAdapter(profile, path);
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

            // Auto-generate default governance config if missing and governance is enabled
            EnsureGovernanceConfigExists(options);

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

        /// <summary>
        /// Ensures a governance config file exists. Creates one with sensible defaults if missing.
        /// </summary>
        private static void EnsureGovernanceConfigExists(CerbiStreamOptions options)
        {
            if (!options.EnableGovernanceChecks) return;

            var configPath = options.GovernanceConfigPath
                ?? Environment.GetEnvironmentVariable("CERBI_GOVERNANCE_PATH")
                ?? Path.Combine(AppContext.BaseDirectory, DefaultGovernanceFileName);

            if (!File.Exists(configPath))
            {
                try
                {
                    File.WriteAllText(configPath, DefaultGovernanceContent);
                    Console.WriteLine($"[CerbiStream] Created default governance config at: {configPath}");
                }
                catch
                {
                    // Silent fail - governance will use in-memory defaults
                }
            }
        }
    }
}
