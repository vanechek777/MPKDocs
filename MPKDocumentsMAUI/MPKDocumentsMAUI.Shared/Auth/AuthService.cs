using MPKDocumentsMAUI.Shared.Api;

namespace MPKDocumentsMAUI.Shared.Auth;

public sealed class AuthService
{
    private readonly AuthApiClient _api;
    private readonly IAuthTokenStore _tokenStore;
    private readonly ApiAuthenticationStateProvider _authState;

    public AuthService(AuthApiClient api, IAuthTokenStore tokenStore, ApiAuthenticationStateProvider authState)
    {
        _api = api;
        _tokenStore = tokenStore;
        _authState = authState;
    }

    public async Task<bool> TryWarmUpAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            await _api.MeAsync();
            _authState.NotifyAuthChanged();
            return true;
        }
        catch
        {
            await LogoutAsync();
            return false;
        }
    }

    public Task<EmailCodeSendResponse> SendEmailLoginCodeAsync(string email) =>
        _api.SendEmailLoginCodeAsync(new EmailLoginSendRequest(NormalizeEmail(email)));

    public async Task LoginWithPasswordAsync(string phoneNumber, string password)
    {
        var token = await _api.LoginAsync(
            new LoginRequest(phoneNumber.Trim(), password));
        await _tokenStore.SetAccessTokenAsync(token.access_token);
        _authState.NotifyAuthChanged();
    }

    public async Task LoginWithEmailCodeAsync(string email, string code)
    {
        var token = await _api.VerifyEmailLoginAsync(
            new EmailLoginVerifyRequest(NormalizeEmail(email), code.Trim()));
        await _tokenStore.SetAccessTokenAsync(token.access_token);
        _authState.NotifyAuthChanged();
    }

    public Task<EmailCodeSendResponse> SendRegisterEmailCodeAsync(string phoneNumber, string fullName,
        string password, string email) =>
        _api.SendRegisterEmailCodeAsync(new RegisterRequest(phoneNumber, fullName, password, NormalizeEmail(email)));

    public async Task CompleteRegistrationWithEmailCodeAsync(string email, string code)
    {
        var token = await _api.VerifyRegisterEmailAsync(
            new RegisterEmailVerifyRequest(NormalizeEmail(email), code.Trim()));
        await _tokenStore.SetAccessTokenAsync(token.access_token);
        _authState.NotifyAuthChanged();
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<MeResponse> GetMeAsync(CancellationToken ct = default) =>
        await _api.MeAsync(ct);

    public async Task<MeResponse> PatchProfileAsync(MePatchRequest patch, CancellationToken ct = default)
    {
        var me = await _api.PatchMeAsync(patch, ct);
        _authState.NotifyAuthChanged();
        return me;
    }

    public async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        _authState.NotifyAuthChanged();
    }
}

