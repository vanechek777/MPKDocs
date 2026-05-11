using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MPKDocumentsMAUI.Shared.Auth;
using MPKDocumentsMAUI.Shared.Utilities;

namespace MPKDocumentsMAUI.Shared.Api;

/// <summary>Результат POST /documents/{id}/actions/sign.</summary>
public sealed record SignDocumentResult(bool Ok, bool InvalidOtp, ActionResponseDto? Data);

public sealed class DocumentsApiClient
{
    private readonly HttpClient _http;
    private readonly ApiOptions _options;
    private readonly IAuthTokenStore _tokenStore;

    public DocumentsApiClient(HttpClient http, ApiOptions options, IAuthTokenStore tokenStore)
    {
        _http = http;
        _options = options;
        _tokenStore = tokenStore;
    }

    private Uri U(string path) => new(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));

    private static async Task<byte[]> ReadHttpContentWithProgressAsync(
        HttpContent content,
        IProgress<(long BytesRead, long? ContentLength)>? progress,
        CancellationToken ct)
    {
        var total = content.Headers.ContentLength;
        progress?.Report((0, total));
        await using var stream = await content.ReadAsStreamAsync(ct);
        const int bufLen = 81920;
        var buffer = new byte[bufLen];
        using var ms = new MemoryStream(total is > 0 and <= int.MaxValue ? (int)total : 0);
        long read = 0;
        while (true)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(0, bufLen), ct);
            if (n == 0)
                break;
            await ms.WriteAsync(buffer.AsMemory(0, n), ct);
            read += n;
            progress?.Report((read, total));
        }

        return ms.ToArray();
    }

    private async Task AttachAuthAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<DocumentListItemDto>> GetRecentAsync(
        string tab,
        string? q,
        int limit = 50,
        bool archive = false,
        bool searchInTemplateNames = true,
        CancellationToken ct = default)
    {
        await AttachAuthAsync();

        var qp = new List<string>
        {
            $"limit={Math.Clamp(limit, 1, 200)}",
            $"tab={Uri.EscapeDataString(string.IsNullOrWhiteSpace(tab) ? "all" : tab)}",
            $"archive={(archive ? "true" : "false")}",
            $"search_in_template_names={(searchInTemplateNames ? "true" : "false")}",
        };
        if (!string.IsNullOrWhiteSpace(q))
            qp.Add($"q={Uri.EscapeDataString(q)}");

        var res = await _http.GetAsync(U("/documents/recent?" + string.Join("&", qp)), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<DocumentListItemDto>>(cancellationToken: ct)) ?? new();
    }

    public async Task<List<DocumentListItemDto>> GetMyDraftsAsync(int limit = 50, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var lim = Math.Clamp(limit, 1, 200);
        var res = await _http.GetAsync(U($"/documents/drafts?limit={lim}"), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<DocumentListItemDto>>(cancellationToken: ct)) ?? new();
    }

    public async Task<List<TemplateListItemDto>> ListTemplatesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var path = "/templates" + (activeOnly ? "?active_only=true" : "?active_only=false");
        var res = await _http.GetAsync(U(path), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<TemplateListItemDto>>(cancellationToken: ct)) ?? new();
    }

    /// <summary>Имена активных категорий из БД (в т.ч. без шаблонов).</summary>
    public async Task<List<string>> ListTemplateCategoryNamesAsync(CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U("/templates/category-names"), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<string>>(cancellationToken: ct)) ?? new();
    }

    public async Task<TemplateDetailDto> GetTemplateAsync(int templateId, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U($"/templates/{templateId}"), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<TemplateDetailDto>(cancellationToken: ct))!;
    }

    public async Task<List<UserListItemDto>> ListUsersAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var path = "/users" + (activeOnly ? "?active_only=true" : "?active_only=false");
        var res = await _http.GetAsync(U(path), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<UserListItemDto>>(cancellationToken: ct)) ?? new();
    }

    public async Task<CreateDocumentResponseDto> CreateDocumentAsync(
        int templateId,
        Dictionary<string, object?> content,
        List<int> signerUserIds,
        int? signerDepartmentId,
        bool saveAsDraft,
        CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var req = new CreateDocumentRequestDto(
            template_id: templateId,
            content: content,
            signer_user_ids: signerUserIds,
            signer_department_id: signerDepartmentId,
            save_as_draft: saveAsDraft
        );
        var res = await _http.PostAsJsonAsync(U("/documents"), req, ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CreateDocumentResponseDto>(cancellationToken: ct))!;
    }

    public async Task UpdateDraftContentAsync(
        int documentId,
        IReadOnlyDictionary<string, object?> content,
        CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var body = new Dictionary<string, object?> { ["content"] = content.ToDictionary(static kv => kv.Key, static kv => kv.Value) };
        using var req = new HttpRequestMessage(HttpMethod.Patch, U($"/documents/{documentId}/draft-content"))
        {
            Content = JsonContent.Create(body),
        };
        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    /// <summary>DELETE /documents/{id}/draft — удалить черновик (только инициатор, статус DRAFT).</summary>
    public async Task DeleteDraftAsync(int documentId, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        using var req = new HttpRequestMessage(HttpMethod.Delete, U($"/documents/{documentId}/draft"));
        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task<List<DepartmentListItemDto>> ListDepartmentsAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var path = "/departments" + (activeOnly ? "?active_only=true" : "?active_only=false");
        var res = await _http.GetAsync(U(path), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<DepartmentListItemDto>>(cancellationToken: ct)) ?? new();
    }

    public async Task<List<SigningListItemDto>> GetSigningInboxAsync(
        DateTime? forDate = null,
        CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var qp = new List<string>();
        if (forDate.HasValue)
            qp.Add($"for_date={forDate.Value:yyyy-MM-dd}");
        var path = "/signing/inbox" + (qp.Count > 0 ? "?" + string.Join("&", qp) : "");
        var res = await _http.GetAsync(U(path), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<SigningListItemDto>>(cancellationToken: ct)) ?? new();
    }

    public async Task<List<SigningListItemDto>> GetSigningSignedAsync(
        DateTime? forDate = null,
        CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var qp = new List<string>();
        if (forDate.HasValue)
            qp.Add($"for_date={forDate.Value:yyyy-MM-dd}");
        var path = "/signing/signed" + (qp.Count > 0 ? "?" + string.Join("&", qp) : "");
        var res = await _http.GetAsync(U(path), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<SigningListItemDto>>(cancellationToken: ct)) ?? new();
    }

    public async Task<List<SigningListItemDto>> GetSigningRejectedByMeAsync(
        DateTime? forDate = null,
        CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var qp = new List<string>();
        if (forDate.HasValue)
            qp.Add($"for_date={forDate.Value:yyyy-MM-dd}");
        var path = "/signing/rejected-by-me" + (qp.Count > 0 ? "?" + string.Join("&", qp) : "");
        var res = await _http.GetAsync(U(path), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<SigningListItemDto>>(cancellationToken: ct)) ?? new();
    }

    public async Task<DocumentDetailDto> GetDetailAsync(int documentId, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U($"/documents/{documentId}"), ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<DocumentDetailDto>(cancellationToken: ct))!;
    }

    /// <summary>POST /documents/{id}/view — отметить просмотр (на главной исчезает голубая точка «не просмотрено»).</summary>
    public async Task RecordDocumentViewAsync(int documentId, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        using var req = new HttpRequestMessage(HttpMethod.Post, U($"/documents/{documentId}/view"));
        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    /// <summary>400 с detail «Invalid or expired code» и др. — неверный OTP.</summary>
    public async Task<SignDocumentResult> SignDocumentAsync(int documentId, string? otpCode, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PostAsJsonAsync(
            U($"/documents/{documentId}/actions/sign"),
            new ActionRequestDto(otp_code: otpCode, reason: null),
            ct
        );

        if (res.StatusCode == HttpStatusCode.BadRequest)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            var looksLikeOtp =
                body.Contains("Invalid", StringComparison.OrdinalIgnoreCase)
                || body.Contains("expired", StringComparison.OrdinalIgnoreCase)
                || body.Contains("код", StringComparison.OrdinalIgnoreCase);
            return new SignDocumentResult(Ok: false, InvalidOtp: looksLikeOtp, Data: null);
        }

        if (!res.IsSuccessStatusCode)
            return new SignDocumentResult(Ok: false, InvalidOtp: false, Data: null);

        var data = await res.Content.ReadFromJsonAsync<ActionResponseDto>(cancellationToken: ct);
        return new SignDocumentResult(Ok: true, InvalidOtp: false, Data: data);
    }

    public async Task<ActionResponseDto> SignAsync(int documentId, string? otpCode, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PostAsJsonAsync(
            U($"/documents/{documentId}/actions/sign"),
            new ActionRequestDto(otp_code: otpCode, reason: null),
            ct
        );
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ActionResponseDto>(cancellationToken: ct))!;
    }

    public async Task<ActionResponseDto> RejectAsync(int documentId, string? reason, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PostAsJsonAsync(
            U($"/documents/{documentId}/actions/reject"),
            new ActionRequestDto(otp_code: null, reason: reason),
            ct
        );
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ActionResponseDto>(cancellationToken: ct))!;
    }

    public async Task<ActionResponseDto> CancelAsync(int documentId, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PostAsJsonAsync(
            U($"/documents/{documentId}/actions/cancel"),
            new ActionRequestDto(otp_code: null, reason: null),
            ct
        );
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ActionResponseDto>(cancellationToken: ct))!;
    }

    public async Task<(byte[] Body, string? ContentType)> DownloadDocumentFileAsync(
        int documentId,
        CancellationToken ct = default,
        IProgress<(long BytesRead, long? ContentLength)>? downloadProgress = null)
    {
        await AttachAuthAsync();
        using var res = await _http.GetAsync(
            U($"/documents/{documentId}/file"),
            HttpCompletionOption.ResponseHeadersRead,
            ct);
        res.EnsureSuccessStatusCode();
        var media = res.Content.Headers.ContentType?.MediaType;
        var body = await ReadHttpContentWithProgressAsync(res.Content, downloadProgress, ct);
        return (body, media);
    }

    public async Task UploadDocumentFileAsync(
        int documentId,
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken ct = default,
        IProgress<(long bytesSent, long bytesTotal)>? uploadProgress = null)
    {
        await AttachAuthAsync();
        using var form = new MultipartFormDataContent();
        Stream body = fileStream;
        if (uploadProgress is not null && fileStream.CanSeek && fileStream.Length > 0)
            body = new ProgressReportingReadStream(fileStream, fileStream.Length, uploadProgress);
        using var streamContent = new StreamContent(body);
        if (!string.IsNullOrWhiteSpace(contentType))
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", fileName);
        var res = await _http.PostAsync(U($"/documents/{documentId}/file"), form, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> DownloadMyNepEsigAsync(
        int documentId,
        CancellationToken ct = default,
        IProgress<(long BytesRead, long? ContentLength)>? downloadProgress = null)
    {
        await AttachAuthAsync();
        using var res = await _http.GetAsync(
            U($"/documents/{documentId}/nep-signature.esig"),
            HttpCompletionOption.ResponseHeadersRead,
            ct);
        res.EnsureSuccessStatusCode();
        return await ReadHttpContentWithProgressAsync(res.Content, downloadProgress, ct);
    }

    public async Task<VerifyEsigResponseDto> VerifyEsigAsync(Stream esigStream, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(esigStream), "file", "signature.esig");
        var res = await _http.PostAsync(U("/signatures/verify-esig"), form, ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<VerifyEsigResponseDto>(cancellationToken: ct))!;
    }

    public async Task<VerifyEsigResponseDto> VerifyNepQrPayloadAsync(VerifyQrPayloadDto payload, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PostAsJsonAsync(U("/signatures/verify-qr-payload"), payload, cancellationToken: ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<VerifyEsigResponseDto>(cancellationToken: ct))!;
    }
}

