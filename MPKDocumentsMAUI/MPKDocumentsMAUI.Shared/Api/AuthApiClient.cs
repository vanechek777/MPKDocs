using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MPKDocumentsMAUI.Shared.Auth;

namespace MPKDocumentsMAUI.Shared.Api;

public sealed class AuthApiClient
{
    private readonly HttpClient _http;
    private readonly ApiOptions _options;
    private readonly IAuthTokenStore _tokenStore;

    public AuthApiClient(HttpClient http, ApiOptions options, IAuthTokenStore tokenStore)
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

    /// <summary>Запрос отправки OTP (SMS через SMSC на сервере).</summary>
    public async Task<OtpSendResponse> SendOtpAsync(CancellationToken ct = default)
    {
        await AttachAuthAsync();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var res = await _http.PostAsync(U("/auth/otp/send"), content, ct);
        if (!res.IsSuccessStatusCode)
            await ThrowIfFailedAsync(res, ct);

        return (await res.Content.ReadFromJsonAsync<OtpSendResponse>(cancellationToken: ct))
               ?? new OtpSendResponse(Ok: true, DevCode: null);
    }

    private static async Task<string> ReadFastApiDetailAsync(HttpResponseMessage res, CancellationToken ct)
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
        var raw = await ReadFastApiDetailAsync(res, ct);
        throw new HttpRequestException(HttpApiErrorFormatter.Humanize(res.StatusCode, raw));
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(U("/auth/login"), req, ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct))!;
    }

    public async Task<TokenResponse> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(U("/auth/register"), req, ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct))!;
    }

    /// <summary>Отправить код входа на email.</summary>
    public async Task<EmailCodeSendResponse> SendEmailLoginCodeAsync(EmailLoginSendRequest req,
        CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(U("/auth/email/login/send"), req, ct);
        await ThrowIfFailedAsync(res, ct);

        return (await res.Content.ReadFromJsonAsync<EmailCodeSendResponse>(cancellationToken: ct))
               ?? new EmailCodeSendResponse(Ok: true, DevCode: null);
    }

    /// <summary>Обмен кода входа на JWT.</summary>
    public async Task<TokenResponse> VerifyEmailLoginAsync(EmailLoginVerifyRequest req,
        CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(U("/auth/email/login/verify"), req, ct);
        await ThrowIfFailedAsync(res, ct);

        return (await res.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct))!;
    }

    /// <summary>Отправить код подтверждения регистрации на email (аккаунт активируется после verify).</summary>
    public async Task<EmailCodeSendResponse> SendRegisterEmailCodeAsync(RegisterRequest pending,
        CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(U("/auth/email/register/start"), pending, ct);
        await ThrowIfFailedAsync(res, ct);

        return (await res.Content.ReadFromJsonAsync<EmailCodeSendResponse>(cancellationToken: ct))
               ?? new EmailCodeSendResponse(Ok: true, DevCode: null);
    }

    /// <summary>Завершить регистрацию по коду с почты, получить JWT.</summary>
    public async Task<TokenResponse> VerifyRegisterEmailAsync(RegisterEmailVerifyRequest req,
        CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(U("/auth/email/register/verify"), req, ct);
        await ThrowIfFailedAsync(res, ct);

        return (await res.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct))!;
    }

    public async Task<MeResponse> MeAsync(CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.GetAsync(U("/users/me"), ct);
        await ThrowIfFailedAsync(res, ct);
        return (await res.Content.ReadFromJsonAsync<MeResponse>(cancellationToken: ct))!;
    }

    public async Task<MeResponse> PatchMeAsync(MePatchRequest req, CancellationToken ct = default)
    {
        await AttachAuthAsync();
        var res = await _http.PatchAsJsonAsync(U("/users/me"), req, ct);
        await ThrowIfFailedAsync(res, ct);

        return (await res.Content.ReadFromJsonAsync<MeResponse>(cancellationToken: ct))!;
    }
}

