// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class DefaultOidcOptionsConfiguration : IPostConfigureOptions<RemoteAuthenticationOptions<OidcProviderOptions>>
{
    private readonly NavigationManager _navigationManager;

    public DefaultOidcOptionsConfiguration(NavigationManager navigationManager) => _navigationManager = navigationManager;

    public void Configure(RemoteAuthenticationOptions<OidcProviderOptions> options)
    {
        options.UserOptions.AuthenticationType ??= options.ProviderOptions.ClientId;

        var redirectUri = options.ProviderOptions.RedirectUri;
        if (redirectUri == null || !Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
        {
            redirectUri ??= "authentication/login-callback";
            options.ProviderOptions.RedirectUri = _navigationManager
                .ToAbsoluteUri(redirectUri).AbsoluteUri;
        }

        var logoutUri = options.ProviderOptions.PostLogoutRedirectUri;
        if (logoutUri == null || !Uri.TryCreate(logoutUri, UriKind.Absolute, out _))
        {
            logoutUri ??= "authentication/logout-callback";
            options.ProviderOptions.PostLogoutRedirectUri = _navigationManager
                .ToAbsoluteUri(logoutUri).AbsoluteUri;
        }
    }

    public void PostConfigure(string? name, RemoteAuthenticationOptions<OidcProviderOptions> options)
    {
        if (string.Equals(name, Options.DefaultName))
        {
            Configure(options);
        }
    }
}
