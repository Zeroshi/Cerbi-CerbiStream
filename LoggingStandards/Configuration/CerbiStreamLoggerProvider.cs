using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace CerbiStream.Configuration
{
    public class CerbiStreamLoggerProvider : ILoggerProvider
    {
        private readonly CerbiStreamOptions _options;
        private readonly ConcurrentDictionary<string, CerbiStreamLoggerAdapter> _loggers = new();

        public CerbiStreamLoggerProvider(CerbiStreamOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new CerbiStreamLoggerAdapter(name, _options));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
