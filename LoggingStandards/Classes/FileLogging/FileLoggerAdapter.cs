using Microsoft.Extensions.Logging;
using System;

namespace CerbiStream.FileLogging
{
    public class FileLoggerAdapter : ILogger
    {
        private readonly ResilientFileLogger _resilientLogger;
        private readonly string _category;

        public FileLoggerAdapter(ResilientFileLogger resilientLogger, string category)
        {
            _resilientLogger = resilientLogger;
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                Category = _category,
                Level = logLevel.ToString(),
                Message = formatter(state, exception),
                Exception = exception?.ToString()
            };

            _resilientLogger.Log(logEntry);
        }
    }
}
