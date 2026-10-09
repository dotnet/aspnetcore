// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = $"{builder.HostEnvironment.BaseAddress}oidc";
    options.ProviderOptions.ClientId = "s6BhdRkqt3";
    options.ProviderOptions.ResponseType = "code";
});

// A test selects the fragment-configured client by adding 'oidcClient=fragment' to the page URL.
// The OIDC callbacks are full page loads, so the redirect URIs of this client carry the same query
// string to keep the selection across them. The authorization code flow is kept: once a response
// mode is configured it alone defines where the callback parameters are.
builder.Services.AddOptions<RemoteAuthenticationOptions<OidcProviderOptions>>()
    .Configure<NavigationManager>((options, navigation) =>
    {
        if (!new Uri(navigation.Uri).Query.Contains("oidcClient=fragment", StringComparison.Ordinal))
        {
            return;
        }

        options.ProviderOptions.ResponseMode = "fragment";
        options.ProviderOptions.RedirectUri = "authentication/login-callback?oidcClient=fragment";
        options.ProviderOptions.PostLogoutRedirectUri = "authentication/logout-callback?oidcClient=fragment";
    });

await builder.Build().RunAsync();
