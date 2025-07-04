using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CerbiStream.Infrastructure
{
    public class AsyncConsoleDispatcher : IDisposable
    {
        private readonly Channel<string> _channel;
        private readonly Task _worker;
        private readonly CancellationTokenSource _cts = new();

        public AsyncConsoleDispatcher(int capacity = 8192)
        {
            _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
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
                    // Fallback logging?
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
