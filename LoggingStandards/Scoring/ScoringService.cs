using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Cerbi.Contracts.Contracts;

namespace CerbiStream.Scoring
{
    /// <summary>
    /// Options for score shipping.
    /// </summary>
    public class ScoringOptions
    {
        public bool Enabled { get; set; } = true;
        public bool LicenseAllowsScoring { get; set; } = true;
        public int MaxQueueSize { get; set; } = 10_000;
        public int FlushIntervalSeconds { get; set; } = 5;
        public int BatchSize { get; set; } = 100;
    }

    /// <summary>
    /// Options for Service Bus connection.
    /// </summary>
    public class ServiceBusOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string QueueName { get; set; } = "cerbishield.log-scoring";
    }

    /// <summary>
    /// Interface for score shipping.
    /// </summary>
    public interface IScoringService : IDisposable
    {
        void Enqueue(ScoringEventDto ev);
    }

    /// <summary>
    /// High-throughput score shipper with batching for Azure Service Bus.
    /// </summary>
    public sealed class ScoringService : IScoringService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ScoringOptions _options;
        private readonly ServiceBusOptions _sbOptions;
        private readonly ConcurrentQueue<ScoringEventDto> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _worker;
        private readonly ServiceBusClient? _serviceBusClient;
        private readonly ServiceBusSender? _serviceBusSender;

        public ScoringService(ScoringOptions options, ServiceBusOptions sbOptions)
        {
            _options = options ?? new ScoringOptions();
            _sbOptions = sbOptions ?? new ServiceBusOptions();
            (_serviceBusClient, _serviceBusSender) = CreateServiceBusSender(_sbOptions);
            _worker = Task.Run(WorkerLoop);
        }

        public void Enqueue(ScoringEventDto ev)
        {
            if (!_options.Enabled || !_options.LicenseAllowsScoring) return;
            if (_queue.Count >= _options.MaxQueueSize) return;
            _queue.Enqueue(ev);
        }

        private async Task WorkerLoop()
        {
            var flushInterval = TimeSpan.FromSeconds(Math.Max(1, _options.FlushIntervalSeconds));

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(flushInterval, _cts.Token).ConfigureAwait(false);
                    await FlushAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogInternal("[CerbiStream.Scoring] Worker error: {0}", ex.Message);
                }
            }

            // Final flush on shutdown
            try { await FlushAsync().ConfigureAwait(false); } catch { }
        }

        private async Task FlushAsync()
        {
            if (_queue.IsEmpty) return;

            var batch = new System.Collections.Generic.List<ScoringEventDto>(_options.BatchSize);
            while (batch.Count < _options.BatchSize && _queue.TryDequeue(out var ev))
            {
                batch.Add(ev);
            }

            if (batch.Count == 0) return;

            await SendToServiceBusAsync(batch).ConfigureAwait(false);
        }

        private async Task SendToServiceBusAsync(System.Collections.Generic.List<ScoringEventDto> batch)
        {
            if (_serviceBusSender == null)
            {
                LogInternal("[CerbiStream.Scoring] Service Bus sender not configured, dropping {0} events", batch.Count);
                return;
            }

            try
            {
                foreach (var ev in batch)
                {
                    var payload = JsonSerializer.Serialize(ev, SerializerOptions);
                    var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(payload))
                    {
                        ContentType = "application/json",
                        MessageId = ev.LogId ?? Guid.NewGuid().ToString("N"),
                        CorrelationId = ev.CorrelationId,
                        Subject = ev.GovernanceProfile
                    };
                    await _serviceBusSender.SendMessageAsync(message, _cts.Token).ConfigureAwait(false);
                }
                LogInternal("[CerbiStream.Scoring] Sent {0} events to Service Bus", batch.Count);
            }
            catch (Exception ex)
            {
                LogInternal("[CerbiStream.Scoring] Service Bus send failed: {0}", ex.Message);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _worker.Wait(2000); } catch { }
            _cts.Dispose();

            try { _serviceBusSender?.DisposeAsync().AsTask().Wait(500); } catch { }
            try { _serviceBusClient?.DisposeAsync().AsTask().Wait(500); } catch { }
        }

        private static void LogInternal(string message, params object?[] args)
        {
            try
            {
                var formatted = (args != null && args.Length > 0)
                    ? string.Format(CultureInfo.InvariantCulture, message, args)
                    : message;
                Console.WriteLine(formatted);
                Trace.WriteLine(formatted);
            }
            catch { }
        }

        private static (ServiceBusClient? Client, ServiceBusSender? Sender) CreateServiceBusSender(ServiceBusOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString) || string.IsNullOrWhiteSpace(options.QueueName))
            {
                LogInternal("[CerbiStream.Scoring] Service Bus not configured");
                return (null, null);
            }

            try
            {
                var client = new ServiceBusClient(options.ConnectionString);
                var sender = client.CreateSender(options.QueueName);
                LogInternal("[CerbiStream.Scoring] Connected to Service Bus queue: {0}", options.QueueName);
                return (client, sender);
            }
            catch (Exception ex)
            {
                LogInternal("[CerbiStream.Scoring] Failed to create Service Bus sender: {0}", ex.Message);
                return (null, null);
            }
        }
    }
}
