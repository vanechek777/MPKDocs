using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using MPKDocumentsMAUI.Shared.Api;

namespace MPKDocumentsMAUI.Shared.Auth;

public sealed class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthTokenStore _tokenStore;
    private readonly AuthApiClient _api;

    public ApiAuthenticationStateProvider(IAuthTokenStore tokenStore, AuthApiClient api)
    {
        _tokenStore = tokenStore;
        _api = api;
    }

    public void NotifyAuthChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        try
        {
            var me = await _api.MeAsync();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, me.id.ToString()),
                new(ClaimTypes.Name, me.full_name),
                new("phone_number", me.phone_number),
            };
            if (!string.IsNullOrWhiteSpace(me.email))
                claims.Add(new Claim(ClaimTypes.Email, me.email!));

            var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            await _tokenStore.ClearAsync();
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
}

