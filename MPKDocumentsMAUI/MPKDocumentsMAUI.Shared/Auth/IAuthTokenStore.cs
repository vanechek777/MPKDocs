namespace MPKDocumentsMAUI.Shared.Auth;

public interface IAuthTokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task SetAccessTokenAsync(string? token);
    Task ClearAsync();
}

