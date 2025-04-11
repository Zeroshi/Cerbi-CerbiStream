using CerbiStream.Classes.FileLogging;
using Microsoft.Extensions.Logging;
using System;
using FileFallbackOptions = CerbiStream.Classes.FileLogging.FileFallbackOptions;

namespace CerbiStream.FileLogging
{
    public class FileFallbackProvider : ILoggerProvider
    {
        private readonly FileFallbackOptions _options;
        private readonly ResilientFileLogger _resilientLogger;

        public FileFallbackProvider(FileFallbackOptions options)
        {
            _options = options;
            _resilientLogger = new ResilientFileLogger(
                options.PrimaryFilePath,
                options.FallbackFilePath,
                options.RetryCount,
                options.RetryDelay
            );
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLoggerAdapter(_resilientLogger, categoryName);
        }

        public void Dispose()
        {
            // Dispose if needed (e.g., stream cleanup)
        }
    }
}
