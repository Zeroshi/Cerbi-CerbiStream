using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace CerbiStream.Classes.Batching
{
    /// <summary>
    /// A batch logger that collects log messages and sends them as a batch.
    /// </summary>
    public class BatchLogger
    {
        private readonly ConcurrentQueue<string> _logQueue;
        private readonly Timer _flushTimer;
        private readonly int _batchSize;
        private readonly Action<IEnumerable<string>> _sendBatchAction;
        private readonly object _lockObj = new object();

        /// <summary>
        /// Initializes a new instance of the BatchLogger class.
        /// </summary>
        /// <param name="flushIntervalMilliseconds">The interval in milliseconds between flushes.</param>
        /// <param name="batchSize">The maximum number of messages per batch.</param>
        /// <param name="sendBatchAction">An action delegate that handles sending a batch of messages.</param>
        public BatchLogger(int flushIntervalMilliseconds, int batchSize, Action<IEnumerable<string>> sendBatchAction)
        {
            _logQueue = new ConcurrentQueue<string>();
            _batchSize = batchSize;
            _sendBatchAction = sendBatchAction ?? throw new ArgumentNullException(nameof(sendBatchAction));
            _flushTimer = new Timer(flushIntervalMilliseconds) { AutoReset = true };

            _flushTimer.Elapsed += (sender, args) => Flush();
            _flushTimer.Start();
        }

        /// <summary>
        /// Enqueues a log message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _logQueue.Enqueue(message);

            // If the batch size is reached, flush immediately.
            if (_logQueue.Count >= _batchSize)
            {
                Flush();
            }
        }

        /// <summary>
        /// Flushes the queued log messages as a batch.
        /// </summary>
        public void Flush()
        {
            List<string> batch = new List<string>();

            lock (_lockObj)
            {
                while (batch.Count < _batchSize && _logQueue.TryDequeue(out string logMessage))
                {
                    batch.Add(logMessage);
                }
            }

            if (batch.Any())
            {
                _sendBatchAction(batch);
            }
        }

        /// <summary>
        /// Stops the batch logger.
        /// </summary>
        public void Stop()
        {
            _flushTimer.Stop();
            _flushTimer.Dispose();
        }
    }
}
