using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MPKDocumentsMAUI.Shared.Auth;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed class AdminApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly ApiOptions _options;
    private readonly IAuthTokenStore _tokenStore;

    public AdminApiClient(HttpClient http, ApiOptions options, IAuthTokenStore tokenStore)
    {
        _http = http;
        _options = options;
        _tokenStore = tokenStore;
    }

    private Uri U(string path) => new(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));

    private async Task AttachAuthAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<string> ReadDetailAsync(HttpResponseMessage res, CancellationToken ct)
    {
        try
        {
            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.ValueKind switch
                {
                    JsonValueKind.String => detail.GetString() ?? res.ReasonPhrase ?? "Ошибка",
                    JsonValueKind.Array when detail.GetArrayLength() > 0 => detail[0].ToString(),
                    _ => detail.ToString(),
                };
            }
        }
        catch
        {
            // ignore
        }

        return res.ReasonPhrase ?? "Ошибка запроса";
    }

    private static async Task ThrowIfFailedAsync(HttpResponseMessage res, CancellationToken ct)
    {
        if (res.IsSuccessStatusCode) return;
        var raw = await ReadDetailAsync(res, ct);
        throw new HttpRequestException(HttpApiErrorFormatter.Humanize(res.StatusCode, raw));
    }

    /// <summary>Пинг публичного /health без JWT (измеряется на клиенте).</summary>
    public async Task<long> PingHealthMsAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var res = await _http.GetAsync(U("/health"), ct);
        sw.Stop();
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException(HttpApiErrorFormatter.Humanize(res.StatusCode, await ReadDetailAsync(res, ct)));
        return sw.ElapsedMilliseconds;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U("/admin/dashboard"), ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<AdminDashboardDto>(JsonOpts, ct))!;
    }

    public async Task<List<AdminActivityItemDto>> GetActivityAsync(int limit = 50, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U($"/admin/activity?limit={limit}"), ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<List<AdminActivityItemDto>>(JsonOpts, ct)) ?? [];
    }

    public async Task<List<AdminUserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U("/admin/users"), ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<List<AdminUserDto>>(JsonOpts, ct)) ?? [];
    }

    public async Task<AdminUserDto> SetUserAdminAsync(int userId, bool isAdmin, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PatchAsJsonAsync(U($"/admin/users/{userId}/admin"), new AdminSetAdminRequest(isAdmin), JsonOpts, ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<AdminUserDto>(JsonOpts, ct))!;
    }

    public async Task<AdminUserDto> PromoteByPhoneAsync(string phoneNumber, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PostAsJsonAsync(U("/admin/users/promote-by-phone"), new AdminPromoteByPhoneRequest(phoneNumber.Trim()), JsonOpts, ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<AdminUserDto>(JsonOpts, ct))!;
    }

    public async Task<List<AdminCategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U("/admin/categories"), ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<List<AdminCategoryDto>>(JsonOpts, ct)) ?? [];
    }

    public async Task<AdminCategoryDto> CreateCategoryAsync(string name, int sortOrder = 0, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PostAsJsonAsync(U("/admin/categories"), new AdminCategoryCreateRequest(name, sortOrder), JsonOpts, ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<AdminCategoryDto>(JsonOpts, ct))!;
    }

    public async Task<AdminCategoryDto> PatchCategoryAsync(int id, AdminCategoryPatchRequest body, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PatchAsJsonAsync(U($"/admin/categories/{id}"), body, JsonOpts, ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<AdminCategoryDto>(JsonOpts, ct))!;
    }

    public async Task<List<AdminTemplateDto>> GetTemplatesAsync(CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U("/admin/templates"), ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<List<AdminTemplateDto>>(JsonOpts, ct)) ?? [];
    }

    public async Task<AdminTemplateDto> CreateTemplateAsync(AdminTemplateCreateRequest body, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PostAsJsonAsync(U("/admin/templates"), body, JsonOpts, ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<AdminTemplateDto>(JsonOpts, ct))!;
    }

    public async Task<AdminTemplateDto> PatchTemplateAsync(int id, AdminTemplatePatchRequest body, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PatchAsJsonAsync(U($"/admin/templates/{id}"), body, JsonOpts, ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<AdminTemplateDto>(JsonOpts, ct))!;
    }
}
