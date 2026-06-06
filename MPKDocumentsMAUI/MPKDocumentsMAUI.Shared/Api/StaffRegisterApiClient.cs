using System.Net.Http.Json;
using System.Text.Json;
using MPKDocumentsMAUI.Shared.Auth;

namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Публичные и админские методы кадрового справочника (регистрация, импорт).</summary>
public sealed class StaffRegisterApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _http;
    private readonly ApiOptions _options;
    private readonly IAuthTokenStore _tokenStore;

    public StaffRegisterApiClient(HttpClient http, ApiOptions options, IAuthTokenStore tokenStore)
    {
        _http = http;
        _options = options;
        _tokenStore = tokenStore;
    }

    private Uri U(string path) => new(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));

    public async Task<List<StaffSuggestItem>> SuggestNamesAsync(string q, int limit = 12,
        CancellationToken ct = default)
    {
        var url = $"/public/staff/suggest?q={Uri.EscapeDataString(q ?? "")}&limit={Math.Clamp(limit, 1, 30)}";
        var res = await _http.GetAsync(U(url), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<StaffSuggestItem>>(JsonOpts, ct)) ?? new();
    }

    public async Task<List<StaffPositionItem>> GetPositionsAsync(CancellationToken ct = default)
    {
        var res = await _http.GetAsync(U("/public/staff/positions"), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<StaffPositionItem>>(JsonOpts, ct)) ?? new();
    }

    public async Task<List<StaffDepartmentItem>> GetDepartmentsForPositionAsync(int positionId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(U($"/public/staff/departments?position_id={positionId}"), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<StaffDepartmentItem>>(JsonOpts, ct)) ?? new();
    }

    private async Task AttachAuthAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(token)
                ? null
                : new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<StaffStatsDto> GetStaffStatsAsync(CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U("/admin/staff/stats"), ct);
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException(await res.Content.ReadAsStringAsync(ct));
        return (await res.Content.ReadFromJsonAsync<StaffStatsDto>(JsonOpts, ct))!;
    }

    public async Task<StaffImportResult> ImportStaffFileAsync(Stream fileStream, string fileName,
        CancellationToken ct = default)
    {
        await AttachAuthAsync();
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", fileName);
        var res = await _http.PostAsync(U("/admin/staff/import"), content, ct);
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException(await res.Content.ReadAsStringAsync(ct));
        return (await res.Content.ReadFromJsonAsync<StaffImportResult>(JsonOpts, ct))!;
    }

    public async Task<OneCConfigDto> GetOneCConfigAsync(CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U("/admin/onec/config"), ct);
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException(await res.Content.ReadAsStringAsync(ct));
        return (await res.Content.ReadFromJsonAsync<OneCConfigDto>(JsonOpts, ct))!;
    }

    public async Task<OneCConfigDto> SaveOneCConfigAsync(OneCConfigUpdateRequest req, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PutAsJsonAsync(U("/admin/onec/config"), req, JsonOpts, ct);
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException(await res.Content.ReadAsStringAsync(ct));
        return (await res.Content.ReadFromJsonAsync<OneCConfigDto>(JsonOpts, ct))!;
    }

    public async Task<OneCTestResult> TestOneCAsync(CancellationToken ct = default)
    {
        await AttachAuthAsync();
        using var empty = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var res = await _http.PostAsync(U("/admin/onec/test"), empty, ct);
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException(await res.Content.ReadAsStringAsync(ct));
        return (await res.Content.ReadFromJsonAsync<OneCTestResult>(JsonOpts, ct))!;
    }
}
