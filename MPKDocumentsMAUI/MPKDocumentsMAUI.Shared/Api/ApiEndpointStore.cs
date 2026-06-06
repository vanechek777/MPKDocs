using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed class ApiEndpointStore : IApiEndpointStore
{
    public const string DefaultBaseUrl = "https://mpk-docs.ru.tuna.am";
    private const string StorageKey = "mpk_api_endpoints_v1";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _packagedDefault;
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

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_js is null)
            return;

        try
        {
            var json = await _js.InvokeAsync<string?>("MPKDocuments.settingsGet", cancellationToken, StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var state = JsonSerializer.Deserialize<PersistedState>(json, JsonOpts);
                if (state is not null)
                    ApplyPersisted(state);
            }
        }
        catch
        {
            /* localStorage недоступен */
        }

        _loaded = true;
        Changed?.Invoke();
    }

    public async Task SetActiveAsync(string url, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUrl(url) ?? throw new ArgumentException("Укажите корректный URL.", nameof(url));
        if (_endpoints.All(e => !string.Equals(e.Url, normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Этого адреса нет в списке.");

        _active = normalized;
        await PersistAsync(cancellationToken);
    }

    public async Task AddEndpointAsync(string url, string? label = null, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUrl(url)
                         ?? throw new ArgumentException("Введите полный URL (http:// или https://).", nameof(url));

        if (_endpoints.Any(e => string.Equals(e.Url, normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Такой адрес уже есть в списке.");

        var trimmedLabel = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        _endpoints.Add(new ApiEndpointEntry(normalized, trimmedLabel));
        _active = normalized;
        await PersistAsync(cancellationToken);
    }

    public async Task RemoveEndpointAsync(string url, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUrl(url)
                         ?? throw new ArgumentException("Некорректный URL.", nameof(url));

        if (_endpoints.Count <= 1)
            throw new InvalidOperationException("Нельзя удалить последний адрес.");

        var idx = _endpoints.FindIndex(e => string.Equals(e.Url, normalized, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            throw new InvalidOperationException("Адрес не найден в списке.");

        _endpoints.RemoveAt(idx);
        if (string.Equals(_active, normalized, StringComparison.OrdinalIgnoreCase))
            _active = _endpoints[0].Url;

        await PersistAsync(cancellationToken);
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

    private void ApplyPersisted(PersistedState state)
    {
        var items = new List<ApiEndpointEntry>();
        if (state.Endpoints is { Count: > 0 } list)
        {
            foreach (var e in list)
            {
                var u = NormalizeUrl(e.Url);
                if (u is null)
                    continue;
                if (items.Any(x => string.Equals(x.Url, u, StringComparison.OrdinalIgnoreCase)))
                    continue;
                var lbl = string.IsNullOrWhiteSpace(e.Label) ? null : e.Label.Trim();
                items.Add(new ApiEndpointEntry(u, lbl));
            }
        }

        if (items.Count == 0)
        {
            _endpoints = [new ApiEndpointEntry(_packagedDefault, "По умолчанию")];
            _active = _packagedDefault;
            return;
        }

        _endpoints = items;
        var active = NormalizeUrl(state.ActiveUrl);
        _active = active is not null
                  && items.Any(e => string.Equals(e.Url, active, StringComparison.OrdinalIgnoreCase))
            ? active
            : items[0].Url;
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        if (_js is null)
        {
            Changed?.Invoke();
            return;
        }

        var state = new PersistedState
        {
            ActiveUrl = _active,
            Endpoints = _endpoints
                .Select(e => new PersistedEndpoint { Url = e.Url, Label = e.Label })
                .ToList(),
        };

        try
        {
            await _js.InvokeVoidAsync(
                "MPKDocuments.settingsSet",
                cancellationToken,
                StorageKey,
                JsonSerializer.Serialize(state, JsonOpts));
        }
        catch
        {
            /* ignore */
        }

        Changed?.Invoke();
    }

    private sealed class PersistedState
    {
        public string? ActiveUrl { get; set; }
        public List<PersistedEndpoint>? Endpoints { get; set; }
    }

    private sealed class PersistedEndpoint
    {
        public string Url { get; set; } = "";
        public string? Label { get; set; }
    }
}
