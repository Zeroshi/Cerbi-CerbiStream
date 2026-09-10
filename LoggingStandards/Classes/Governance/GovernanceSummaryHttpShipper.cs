using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CerbiStream.GovernanceRuntime.Governance;

public sealed class GovernanceSummaryHttpOptions : GovernanceSummaryOptions
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public int FlushIntervalSeconds { get; set; } = 30;
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? SendAsync { get; set; }
}

public sealed class GovernanceSummaryHttpShipper : IGovernanceSummarySink, IDisposable, IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly GovernanceSummaryHttpOptions _options;
    private readonly GovernanceSummaryAccumulator _accumulator;
    private readonly Timer? _timer;
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private int _disposed;

    public GovernanceSummaryHttpShipper(GovernanceSummaryHttpOptions options)
    {
        _options = options ?? new GovernanceSummaryHttpOptions();
        _accumulator = new GovernanceSummaryAccumulator(_options);
        if (_options.FlushIntervalSeconds > 0)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(1, _options.FlushIntervalSeconds));
            _timer = new Timer(_ => _ = FlushQuietlyAsync(), null, interval, interval);
        }
    }

    public void Record(IDictionary<string, object> fields, DateTimeOffset? at = null)
        => _accumulator.RecordFields(fields, at);

    public async Task FlushQuietlyAsync(CancellationToken cancellationToken = default)
    {
        try { await FlushAsync(cancellationToken).ConfigureAwait(false); }
        catch { }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            return;

        if (!await _flushLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            var batch = _accumulator.Snapshot();
            if (batch is null)
                return;

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(batch, SerializerOptions), Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                request.Headers.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);

            var response = _options.SendAsync is not null
                ? await _options.SendAsync(request, cancellationToken).ConfigureAwait(false)
                : await SendWithDefaultClientAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"governance summary POST failed with HTTP {(int)response.StatusCode}");

            _accumulator.MarkFlushed(batch);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        _timer?.Dispose();
        try { FlushQuietlyAsync().GetAwaiter().GetResult(); } catch { }
        _flushLock.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        if (_timer is not null)
            await _timer.DisposeAsync().ConfigureAwait(false);
        await FlushQuietlyAsync().ConfigureAwait(false);
        _flushLock.Dispose();
    }

    private static async Task<HttpResponseMessage> SendWithDefaultClientAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
