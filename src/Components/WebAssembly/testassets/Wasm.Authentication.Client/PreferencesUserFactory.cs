// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace Wasm.Authentication.Client;

public class PreferencesUserFactory : AccountClaimsPrincipalFactory<OidcAccount>
{
    private readonly HttpClient _httpClient;

    public PreferencesUserFactory(NavigationManager navigationManager, IAccessTokenProviderAccessor accessor)
        : base(accessor)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
    }

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        OidcAccount account,
        RemoteAuthenticationUserOptions options)
    {
        var initialUser = await base.CreateUserAsync(account, options);

        if (initialUser.Identity.IsAuthenticated)
        {
            foreach (var value in account.AuthenticationMethod)
            {
                ((ClaimsIdentity)initialUser.Identity).AddClaim(new Claim("amr", value));
            }

            var tokenResponse = await TokenProvider.RequestAccessToken();
            if (tokenResponse.TryGetToken(out var token))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "Preferences/HasCompletedAdditionalInformation");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

                var response = await _httpClient.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidOperationException("Error accessing additional user info.");
                }

                var hasInfo = JsonSerializer.Deserialize<bool>(await response.Content.ReadAsStringAsync());
                if (!hasInfo)
                {
                    // The actual pattern would be to cache this info to avoid constant queries to the server per auth update.
                    // (By default once every minute)
                    ((ClaimsIdentity)initialUser.Identity).AddClaim(new Claim("NewUser", "true"));
                }
            }
        }

        return initialUser;
    }
}
