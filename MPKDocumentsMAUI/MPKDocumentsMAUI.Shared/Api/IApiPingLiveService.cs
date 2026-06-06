namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Периодический пинг /health для всех URL из <see cref="IApiEndpointStore"/>.</summary>
public interface IApiPingLiveService
{
    event Action? Changed;

    IReadOnlyDictionary<string, ApiPingResult> Results { get; }

    /// <summary>Запустить цикл (ref-count: несколько подписчиков).</summary>
    void Acquire();

    /// <summary>Остановить цикл, когда подписчиков не осталось.</summary>
    void Release();

    ApiPingResult? TryGet(string baseUrl);

    long? TryGetLatencyMs(string baseUrl);

    Task RefreshNowAsync(CancellationToken cancellationToken = default);
}
