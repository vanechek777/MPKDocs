namespace MPKDocumentsMAUI.Shared.Api;

public sealed class ApiPingLiveService : IApiPingLiveService, IAsyncDisposable
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    private readonly HttpClient _http;
    private readonly IApiEndpointStore _endpoints;
    private readonly object _gate = new();
    private readonly Dictionary<string, ApiPingResult> _results = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _pingGate = new(1, 1);

    private int _refCount;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public ApiPingLiveService(HttpClient http, IApiEndpointStore endpoints)
    {
        _http = http;
        _endpoints = endpoints;
        _endpoints.Changed += OnEndpointsChanged;
    }

    public event Action? Changed;

    public IReadOnlyDictionary<string, ApiPingResult> Results
    {
        get
        {
            lock (_gate)
                return new Dictionary<string, ApiPingResult>(_results, StringComparer.OrdinalIgnoreCase);
        }
    }

    public void Acquire()
    {
        lock (_gate)
        {
            _refCount++;
            if (_refCount == 1)
                StartLoopLocked();
        }
    }

    public void Release()
    {
        lock (_gate)
        {
            if (_refCount <= 0)
                return;
            _refCount--;
            if (_refCount == 0)
                StopLoopLocked();
        }
    }

    public ApiPingResult? TryGet(string baseUrl)
    {
        var key = ApiEndpointStore.NormalizeUrl(baseUrl) ?? baseUrl.Trim();
        lock (_gate)
            return _results.TryGetValue(key, out var r) ? r : null;
    }

    public long? TryGetLatencyMs(string baseUrl)
    {
        var r = TryGet(baseUrl);
        return r is { Ok: true, LatencyMs: { } ms } ? ms : null;
    }

    public Task RefreshNowAsync(CancellationToken cancellationToken = default) =>
        PingAllAsync(cancellationToken);

    private void OnEndpointsChanged()
    {
        lock (_gate)
            _results.Clear();
        Changed?.Invoke();
    }

    private void StartLoopLocked()
    {
        _cts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(_cts.Token);
    }

    private void StopLoopLocked()
    {
        if (_cts is null)
            return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
        _loopTask = null;
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Interval);
        await PingAllAsync(ct);
        while (await timer.WaitForNextTickAsync(ct))
            await PingAllAsync(ct);
    }

    private async Task PingAllAsync(CancellationToken ct)
    {
        if (!await _pingGate.WaitAsync(0, ct))
            return;

        try
        {
            var urls = _endpoints.Endpoints.Select(e => e.Url).ToList();
            var tasks = urls.Select(url => PingOneAsync(url, ct));
            await Task.WhenAll(tasks);
            Changed?.Invoke();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // ignore — следующий тик через 2 с
        }
        finally
        {
            _pingGate.Release();
        }
    }

    private async Task PingOneAsync(string url, CancellationToken ct)
    {
        var result = await ApiHealthPing.PingAsync(_http, url, ct);
        lock (_gate)
        {
            _results[url] = result;
            if (!string.Equals(url, result.BaseUrl, StringComparison.OrdinalIgnoreCase))
                _results[result.BaseUrl] = result;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _endpoints.Changed -= OnEndpointsChanged;
        lock (_gate)
        {
            _refCount = 0;
            StopLoopLocked();
        }

        if (_loopTask is not null)
        {
            try
            {
                await _loopTask;
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        _pingGate.Dispose();
    }
}
