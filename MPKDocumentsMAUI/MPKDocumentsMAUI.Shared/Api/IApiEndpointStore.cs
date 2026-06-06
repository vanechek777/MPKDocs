namespace MPKDocumentsMAUI.Shared.Api;

public sealed record ApiEndpointEntry(string Url, string? Label);

/// <summary>Список базовых URL API и активный адрес (localStorage, ключ <c>mpk_api_endpoints_v1</c>).</summary>
public interface IApiEndpointStore
{
    string ActiveBaseUrl { get; }
    IReadOnlyList<ApiEndpointEntry> Endpoints { get; }
    bool IsLoaded { get; }

    event Action? Changed;

    void AttachJs(Microsoft.JSInterop.IJSRuntime js);

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SetActiveAsync(string url, CancellationToken cancellationToken = default);

    Task AddEndpointAsync(string url, string? label = null, CancellationToken cancellationToken = default);

    Task RemoveEndpointAsync(string url, CancellationToken cancellationToken = default);
}
