using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CerbiStream.Extensions
{
    public static class CerbiLoggerExtensions
    {
        public static CerbiLoggerWrapper Relax(this ILogger logger) => new(logger);

        public static void LogInformation(this ILogger logger, Dictionary<string, object> payload)
        {
            logger.Log(LogLevel.Information, default, payload, null, Format);
        }

        public static void LogWarning(this ILogger logger, Dictionary<string, object> payload)
        {
            logger.Log(LogLevel.Warning, default, payload, null, Format);
        }

        private static string Format(object state, Exception? error) => state?.ToString() ?? string.Empty;
    }

    public class CerbiLoggerWrapper : IDisposable
    {
        private readonly ILogger _logger;

        private static bool _useAsyncConsole = false;
        private static AsyncConsoleDispatcher? _dispatcher;

        public static void EnableAsyncConsole(bool enabled = true, int capacity = 8192)
        {
            _useAsyncConsole = enabled;
            if (_useAsyncConsole && _dispatcher == null)
                _dispatcher = new AsyncConsoleDispatcher(capacity);
        }

        public CerbiLoggerWrapper(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogTrace(string message) => Log(LogLevel.Trace, message);
        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        public void LogInformation(string message) => Log(LogLevel.Information, message);
        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        public void LogError(string message) => Log(LogLevel.Error, message);
        public void LogCritical(string message) => Log(LogLevel.Critical, message);

        private void Log(LogLevel level, string message)
        {
            var metadata = new Dictionary<string, object>
            {
                ["Message"] = message,
                ["GovernanceRelaxed"] = true,
                ["TimestampUtc"] = DateTime.UtcNow
            };

            _logger.Log(level, default, metadata, null, Format);

            var output = $"[{level}] {message}";
            if (_useAsyncConsole && _dispatcher != null)
                _dispatcher.Enqueue(output);
            else
                Console.WriteLine(output);
        }

        private static string Format(object state, Exception? error)
        {
            return state?.ToString() ?? string.Empty;
        }

        public void Dispose()
        {
            _dispatcher?.Dispose();
        }
    }

    internal sealed class AsyncConsoleDispatcher : IDisposable
    {
        private readonly Channel<string> _channel;
        private readonly Task _worker;
        private readonly CancellationTokenSource _cts = new();

        public AsyncConsoleDispatcher(int capacity)
        {
            _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
            {
                SingleWriter = false,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });

            _worker = Task.Run(ProcessAsync);
        }

        public void Enqueue(string message)
        {
            _channel.Writer.TryWrite(message);
        }

        private async Task ProcessAsync()
        {
            await foreach (var msg in _channel.Reader.ReadAllAsync(_cts.Token))
            {
                try
                {
                    await Console.Out.WriteLineAsync(msg);
                }
                catch
                {
                    // Optionally: log to fallback or ignore
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _channel.Writer.TryComplete();
            try { _worker.Wait(); } catch { }
            _cts.Dispose();
        }
    }
}
