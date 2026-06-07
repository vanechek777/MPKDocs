namespace MPKDocumentsMAUI.Shared.Api;

public sealed record ApiEndpointEntry(string Url, string? Label);

/// <summary>
/// Список базовых URL API с сервера (<c>GET /config/api-endpoints</c>);
/// активный адрес — локально (localStorage, ключ <c>mpk_api_active_v1</c>).
/// </summary>
public interface IApiEndpointStore
{
    string ActiveBaseUrl { get; }
    IReadOnlyList<ApiEndpointEntry> Endpoints { get; }
    bool IsLoaded { get; }

    event Action? Changed;

    void AttachJs(Microsoft.JSInterop.IJSRuntime js);

    void AttachHttp(HttpClient http);

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task RefreshFromServerAsync(CancellationToken cancellationToken = default);

    /// <summary>Список из localStorage (старый формат), без изменения текущего состояния.</summary>
    Task<IReadOnlyList<ApiEndpointEntry>> ReadLegacyLocalEndpointsAsync(CancellationToken cancellationToken = default);

    /// <summary>На сервере уже сохранён непустой список хостов (не legacy localStorage).</summary>
    Task<bool> HasServerEndpointsAsync(CancellationToken cancellationToken = default);

    Task SetActiveAsync(string url, CancellationToken cancellationToken = default);

    Task ApplyServerEndpointsAsync(IReadOnlyList<ApiEndpointEntry> endpoints, CancellationToken cancellationToken = default);

    Task AddEndpointAsync(string url, string? label = null, CancellationToken cancellationToken = default);

    Task RemoveEndpointAsync(string url, CancellationToken cancellationToken = default);
}
