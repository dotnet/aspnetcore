// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Authentication.WebAssembly.Msal.Models;
using Microsoft.Extensions.Options;

namespace Microsoft.Authentication.WebAssembly.Msal
{
    internal class MsalDefaultOptionsConfiguration : IPostConfigureOptions<RemoteAuthenticationOptions<MsalProviderOptions>>
    {
        private readonly NavigationManager _navigationManager;

        public MsalDefaultOptionsConfiguration(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public void Configure(RemoteAuthenticationOptions<MsalProviderOptions> options)
        {
            options.UserOptions.ScopeClaim ??= "scp";
            options.UserOptions.AuthenticationType ??= options.ProviderOptions.Authentication.ClientId;

            var redirectUri = options.ProviderOptions.Authentication.RedirectUri;
            if (redirectUri == null || !Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
            {
                redirectUri ??= "authentication/login-callback";
                options.ProviderOptions.Authentication.RedirectUri = _navigationManager
                    .ToAbsoluteUri(redirectUri).AbsoluteUri;
            }

            var logoutUri = options.ProviderOptions.Authentication.PostLogoutRedirectUri;
            if (logoutUri == null || !Uri.TryCreate(logoutUri, UriKind.Absolute, out _))
            {
                logoutUri ??= "authentication/logout-callback";
                options.ProviderOptions.Authentication.PostLogoutRedirectUri = _navigationManager
                    .ToAbsoluteUri(logoutUri).AbsoluteUri;
            }

            options.ProviderOptions.Authentication.NavigateToLoginRequestUrl = false;
        }

        public void PostConfigure(string name, RemoteAuthenticationOptions<MsalProviderOptions> options)
        {
            if (string.Equals(name, Options.DefaultName))
            {
                Configure(options);
            }
        }
    }
}
