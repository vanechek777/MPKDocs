using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed class ApiEndpointStore : IApiEndpointStore
{
    public const string DefaultBaseUrl = "https://mpk-docs.ru";
    private static readonly string[] KnownBootstrapUrls =
    [
        "https://mpk-docs.ru",
        "https://mpk-docs.ru.tuna.am",
    ];
    private const string LegacyStorageKey = "mpk_api_endpoints_v1";
    private const string ActiveStorageKey = "mpk_api_active_v1";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _packagedDefault;
    private HttpClient? _http;
    private IJSRuntime? _js;
    private List<ApiEndpointEntry> _endpoints = [];
    private string _active = DefaultBaseUrl;
    private bool _loaded;

    public ApiEndpointStore(string? packagedDefaultUrl = null)
    {
        _packagedDefault = NormalizeUrl(packagedDefaultUrl) ?? DefaultBaseUrl;
        _active = _packagedDefault;
        _endpoints = [new ApiEndpointEntry(_packagedDefault, "По умолчанию")];
    }

    public string ActiveBaseUrl => _active;

    public IReadOnlyList<ApiEndpointEntry> Endpoints => _endpoints;

    public bool IsLoaded => _loaded;

    public event Action? Changed;

    public void AttachJs(IJSRuntime js) => _js = js;

    public void AttachHttp(HttpClient http) => _http = http;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await RefreshFromServerAsync(cancellationToken);
    }

    public async Task RefreshFromServerAsync(CancellationToken cancellationToken = default)
    {
        var fromServer = await TryFetchServerEndpointsAsync(cancellationToken);
        if (fromServer is { Count: > 0 })
        {
            _endpoints = fromServer;
            EnsurePackagedDefaultInList();
        }
        else
        {
            await TryLoadLegacyLocalListAsync(cancellationToken);
            EnsurePackagedDefaultInList();
        }

        await RestoreActiveFromLocalAsync(cancellationToken);
        _loaded = true;
        Changed?.Invoke();
    }

    public async Task<bool> HasServerEndpointsAsync(CancellationToken cancellationToken = default)
    {
        var fromServer = await TryFetchServerEndpointsAsync(cancellationToken);
        return fromServer is { Count: > 0 };
    }

    public async Task<IReadOnlyList<ApiEndpointEntry>> ReadLegacyLocalEndpointsAsync(
        CancellationToken cancellationToken = default)
    {
        if (_js is null)
            return [];

        try
        {
            var json = await _js.InvokeAsync<string?>("MPKDocuments.settingsGet", cancellationToken, LegacyStorageKey);
            if (string.IsNullOrWhiteSpace(json))
                return [];

            var state = JsonSerializer.Deserialize<LegacyPersistedState>(json, JsonOpts);
            if (state?.Endpoints is not { Count: > 0 })
                return [];

            return NormalizeEntries(state.Endpoints.Select(e => new ApiEndpointEntry(e.Url, e.Label)));
        }
        catch
        {
            return [];
        }
    }

    public async Task SetActiveAsync(string url, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUrl(url) ?? throw new ArgumentException("Укажите корректный URL.", nameof(url));
        if (_endpoints.All(e => !string.Equals(e.Url, normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Этого адреса нет в списке.");

        _active = normalized;
        await PersistActiveAsync(cancellationToken);
    }

    public Task AddEndpointAsync(string url, string? label = null, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Список серверов задаётся администратором на сервере. Используйте админ-панель.");
    }

    public Task RemoveEndpointAsync(string url, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Список серверов задаётся администратором на сервере. Используйте админ-панель.");
    }

    public async Task ApplyServerEndpointsAsync(
        IReadOnlyList<ApiEndpointEntry> endpoints,
        CancellationToken cancellationToken = default)
    {
        var items = NormalizeEntries(endpoints);
        if (items.Count == 0)
            throw new InvalidOperationException("Должен остаться хотя бы один адрес API.");

        _endpoints = items;
        if (_endpoints.All(e => !string.Equals(e.Url, _active, StringComparison.OrdinalIgnoreCase)))
            _active = _endpoints[0].Url;

        await PersistActiveAsync(cancellationToken);
        _loaded = true;
        Changed?.Invoke();
    }

    public static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
            return null;

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    private async Task<List<ApiEndpointEntry>?> TryFetchServerEndpointsAsync(CancellationToken cancellationToken)
    {
        if (_http is null)
            return null;

        foreach (var baseUrl in BootstrapUrls())
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(20));

                var uri = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "config/api-endpoints");
                var response = await _http.GetFromJsonAsync<ServerEndpointsPayload>(uri, JsonOpts, cts.Token);
                if (response?.Endpoints is not { Count: > 0 } list)
                    continue;

                var normalized = NormalizeEntries(list.Select(e => new ApiEndpointEntry(e.Url, e.Label)));
                if (normalized.Count > 0)
                    return normalized;
            }
            catch
            {
                /* пробуем следующий bootstrap URL */
            }
        }

        return null;
    }

    private IEnumerable<string> BootstrapUrls()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string?> candidates =
        [
            _active,
            _packagedDefault,
            .._endpoints.Select(e => e.Url),
        ];

        foreach (var candidate in candidates.Concat(KnownBootstrapUrls))
        {
            var u = NormalizeUrl(candidate);
            if (u is null || !seen.Add(u))
                continue;
            yield return u;
        }
    }

    private async Task TryLoadLegacyLocalListAsync(CancellationToken cancellationToken)
    {
        if (_js is null)
            return;

        try
        {
            var json = await _js.InvokeAsync<string?>("MPKDocuments.settingsGet", cancellationToken, LegacyStorageKey);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var state = JsonSerializer.Deserialize<LegacyPersistedState>(json, JsonOpts);
            if (state?.Endpoints is not { Count: > 0 })
                return;

            var items = NormalizeEntries(state.Endpoints.Select(e => new ApiEndpointEntry(e.Url, e.Label)));
            if (items.Count > 0)
                _endpoints = items;
        }
        catch
        {
            /* localStorage недоступен */
        }
    }

    private void EnsurePackagedDefaultInList()
    {
        if (_endpoints.Any(e => string.Equals(e.Url, _packagedDefault, StringComparison.OrdinalIgnoreCase)))
            return;

        _endpoints.Insert(0, new ApiEndpointEntry(_packagedDefault, "По умолчанию"));
    }

    private async Task RestoreActiveFromLocalAsync(CancellationToken cancellationToken)
    {
        string? active = null;
        if (_js is not null)
        {
            try
            {
                active = await _js.InvokeAsync<string?>("MPKDocuments.settingsGet", cancellationToken, ActiveStorageKey);
                if (string.IsNullOrWhiteSpace(active))
                {
                    var legacyJson = await _js.InvokeAsync<string?>("MPKDocuments.settingsGet", cancellationToken, LegacyStorageKey);
                    if (!string.IsNullOrWhiteSpace(legacyJson))
                    {
                        var legacy = JsonSerializer.Deserialize<LegacyPersistedState>(legacyJson, JsonOpts);
                        active = legacy?.ActiveUrl;
                    }
                }
            }
            catch
            {
                /* ignore */
            }
        }

        var normalized = NormalizeUrl(active);
        _active = normalized is not null
                  && _endpoints.Any(e => string.Equals(e.Url, normalized, StringComparison.OrdinalIgnoreCase))
            ? normalized
            : _endpoints[0].Url;
    }

    private async Task PersistActiveAsync(CancellationToken cancellationToken)
    {
        if (_js is not null)
        {
            try
            {
                await _js.InvokeVoidAsync("MPKDocuments.settingsSet", cancellationToken, ActiveStorageKey, _active);
            }
            catch
            {
                /* ignore */
            }
        }

        Changed?.Invoke();
    }

    private static List<ApiEndpointEntry> NormalizeEntries(IEnumerable<ApiEndpointEntry> source)
    {
        var items = new List<ApiEndpointEntry>();
        foreach (var e in source)
        {
            var u = NormalizeUrl(e.Url);
            if (u is null)
                continue;
            if (items.Any(x => string.Equals(x.Url, u, StringComparison.OrdinalIgnoreCase)))
                continue;
            var lbl = string.IsNullOrWhiteSpace(e.Label) ? null : e.Label.Trim();
            items.Add(new ApiEndpointEntry(u, lbl));
        }

        return items;
    }

    private sealed class ServerEndpointsPayload
    {
        public List<ServerEndpoint>? Endpoints { get; set; }
    }

    private sealed class ServerEndpoint
    {
        public string Url { get; set; } = "";
        public string? Label { get; set; }
    }

    private sealed class LegacyPersistedState
    {
        public string? ActiveUrl { get; set; }
        public List<LegacyEndpoint>? Endpoints { get; set; }
    }

    private sealed class LegacyEndpoint
    {
        public string Url { get; set; } = "";
        public string? Label { get; set; }
    }
}
