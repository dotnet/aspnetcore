// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace BasicTestApp.AuthTest
{
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
            var data = await _httpClient.GetJsonAsync<ClientSideAuthenticationStateData>(uri.AbsoluteUri);
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
}
