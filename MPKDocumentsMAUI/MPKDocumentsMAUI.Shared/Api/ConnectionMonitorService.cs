namespace MPKDocumentsMAUI.Shared.Api;

public sealed class ConnectionMonitorService : IConnectionMonitorService, IAsyncDisposable
{
    public const int PoorLatencyMs = 500;
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);

    private readonly HttpClient _http;
    private readonly IApiEndpointStore _endpoints;
    private CancellationTokenSource? _cts;
    private int _refCount;
    private bool _hasConnectionProblem;

    public ConnectionMonitorService(HttpClient http, IApiEndpointStore endpoints)
    {
        _http = http;
        _endpoints = endpoints;
        _endpoints.Changed += OnEndpointsChanged;
    }

    public event Action? Changed;

    public int? LatencyMs { get; private set; }
    public bool IsPoor => LatencyMs is > PoorLatencyMs;
    public bool HasConnectionProblem => _hasConnectionProblem;

    public void Start()
    {
        if (_refCount++ == 0)
        {
            _cts = new CancellationTokenSource();
            _ = RunAsync(_cts.Token);
        }
    }

    public void Stop()
    {
        if (_refCount <= 0) return;
        if (--_refCount == 0)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnEndpointsChanged() => _ = PingOnceAsync();

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Interval);
        await PingOnceAsync();
        while (await timer.WaitForNextTickAsync(ct))
            await PingOnceAsync();
    }

    private async Task PingOnceAsync()
    {
        int? nextLatency;
        try
        {
            var r = await ApiHealthPing.PingAsync(_http, _endpoints.ActiveBaseUrl);
            nextLatency = r.Ok && r.LatencyMs is { } ms ? (int)Math.Min(ms, int.MaxValue) : null;
        }
        catch
        {
            nextLatency = null;
        }

        LatencyMs = nextLatency;
        var problem = nextLatency is null or > PoorLatencyMs;
        if (problem == _hasConnectionProblem)
            return;

        _hasConnectionProblem = problem;
        Changed?.Invoke();
    }

    public ValueTask DisposeAsync()
    {
        _endpoints.Changed -= OnEndpointsChanged;
        _refCount = 0;
        _cts?.Cancel();
        _cts?.Dispose();
        return ValueTask.CompletedTask;
    }
}
