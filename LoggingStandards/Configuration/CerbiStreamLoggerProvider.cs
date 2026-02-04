using CerbiStream.Logging.Configuration;
using CerbiStream.Scoring;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace CerbiStream.Configuration
{
    public class CerbiStreamLoggerProvider : ILoggerProvider
    {
        private readonly CerbiStreamOptions _options;
        private readonly IScoringService? _ScoringService;
        private readonly ConcurrentDictionary<string, CerbiStreamLoggerAdapter> _loggers = new();

        public CerbiStreamLoggerProvider(CerbiStreamOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            Console.WriteLine($"[CerbiStream] Provider init - DisableQueueSending={_options.DisableQueueSending}, QueueHost={!string.IsNullOrWhiteSpace(_options.QueueHost)}, QueueName={_options.QueueName}");

            // Create ScoringService if queue sending is enabled
            if (!_options.DisableQueueSending && !string.IsNullOrWhiteSpace(_options.QueueHost))
            {
                Console.WriteLine($"[CerbiStream] Creating ScoringService for queue: {_options.QueueName}");
                var shippingOptions = new ScoringOptions
                {
                    Enabled = true,
                    LicenseAllowsScoring = true
                };
                var sbOptions = new ServiceBusOptions
                {
                    ConnectionString = _options.QueueHost,
                    QueueName = _options.QueueName ?? "cerbishield.log-scoring"
                };
                _ScoringService = new ScoringService(shippingOptions, sbOptions);
            }
            else
            {
                Console.WriteLine("[CerbiStream] ScoringService NOT created - queue disabled or no connection string");
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new CerbiStreamLoggerAdapter(name, _options, _ScoringService));
        }

        public void Dispose()
        {
            _loggers.Clear();
            _ScoringService?.Dispose();
        }
    }
}
