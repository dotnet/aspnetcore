// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace BasicTestApp.AuthTest;

// This is intended to be similar to the authentication stateprovider included by default
// with the client-side Blazor "Hosted in ASP.NET Core" template
public class ServerAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;

    public ServerAuthenticationStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var uri = new Uri(_httpClient.BaseAddress, "/subdir/api/User");
        var data = await _httpClient.GetFromJsonAsync<ClientSideAuthenticationStateData>(uri.AbsoluteUri);
        ClaimsIdentity identity;
        if (data.IsAuthenticated)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, data.UserName) }
                .Concat(data.ExposedClaims.Select(c => new Claim(c.Type, c.Value)));
            identity = new ClaimsIdentity(claims, "Server authentication");
        }
        else
        {
            identity = new ClaimsIdentity();
        }

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
