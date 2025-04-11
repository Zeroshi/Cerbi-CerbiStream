using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CerbiStream.FileLogging
{
    public class ResilientFileLogger
    {
        private readonly string _primaryPath;
        private readonly string _fallbackPath;
        private readonly int _retryCount;
        private readonly TimeSpan _retryDelay;

        public ResilientFileLogger(string primaryPath, string fallbackPath, int retryCount, TimeSpan retryDelay)
        {
            _primaryPath = primaryPath;
            _fallbackPath = fallbackPath;
            _retryCount = retryCount;
            _retryDelay = retryDelay;
        }

        public void Log(object logEntry)
        {
            var json = JsonSerializer.Serialize(logEntry);
            for (int i = 0; i < _retryCount; i++)
            {
                try
                {
                    File.AppendAllText(_primaryPath, json + Environment.NewLine);
                    return;
                }
                catch
                {
                    Thread.Sleep(_retryDelay);
                }
            }

            try
            {
                File.AppendAllText(_fallbackPath, json + Environment.NewLine);
            }
            catch
            {
                // Final fail-silent fallback
            }
        }
    }
}
