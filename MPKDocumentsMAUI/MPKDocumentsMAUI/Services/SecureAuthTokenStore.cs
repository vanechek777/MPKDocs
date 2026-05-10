using MPKDocumentsMAUI.Shared.Auth;

namespace MPKDocumentsMAUI.Services;

public sealed class SecureAuthTokenStore : IAuthTokenStore
{
    private const string Key = "mpk_access_token";

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var secure = await SecureStorage.Default.GetAsync(Key);
            if (!string.IsNullOrWhiteSpace(secure)) return secure;
        }
        catch
        {
            // fall through
        }

        // Fallback for platforms where SecureStorage is unavailable (common on Windows dev).
        try
        {
            var pref = Preferences.Default.Get(Key, string.Empty);
            return string.IsNullOrWhiteSpace(pref) ? null : pref;
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAccessTokenAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            await ClearAsync();
            return;
        }

        try
        {
            await SecureStorage.Default.SetAsync(Key, token);
            try { Preferences.Default.Remove(Key); } catch { /* ignore */ }
        }
        catch
        {
            // Fallback
            try { Preferences.Default.Set(Key, token); } catch { /* ignore */ }
        }
    }

    public Task ClearAsync()
    {
        try
        {
            SecureStorage.Default.Remove(Key);
        }
        catch
        {
            // ignore
        }

        try { Preferences.Default.Remove(Key); } catch { /* ignore */ }

        return Task.CompletedTask;
    }
}

