namespace MPKDocumentsMAUI.Shared.Api;

public sealed class DocumentFeedWatchService : IDocumentFeedWatchService, IAsyncDisposable
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(6);

    private readonly DocumentsApiClient _docs;
    private CancellationTokenSource? _cts;
    private int _refCount;

    public DocumentFeedWatchService(DocumentsApiClient docs) => _docs = docs;

    public event Action? FeedChanged;

    public string? LastStamp { get; private set; }

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

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Interval);
        await PollAsync(ct);
        while (await timer.WaitForNextTickAsync(ct))
            await PollAsync(ct);
    }

    private async Task PollAsync(CancellationToken ct)
    {
        try
        {
            var stamp = await _docs.GetFeedStampAsync(ct);
            if (stamp != LastStamp)
            {
                LastStamp = stamp;
                FeedChanged?.Invoke();
            }
        }
        catch
        {
            /* нет токена или сеть */
        }
    }

    public ValueTask DisposeAsync()
    {
        _refCount = 0;
        _cts?.Cancel();
        _cts?.Dispose();
        return ValueTask.CompletedTask;
    }
}
