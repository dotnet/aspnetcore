// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.AzureAD.UI
{
    internal class CookieOptionsConfiguration : IConfigureNamedOptions<CookieAuthenticationOptions>
    {
        private readonly IOptions<AzureADSchemeOptions> _schemeOptions;
        private readonly IOptionsMonitor<AzureADOptions> _AzureADOptions;

        public CookieOptionsConfiguration(IOptions<AzureADSchemeOptions> schemeOptions, IOptionsMonitor<AzureADOptions> AzureADOptions)
        {
            _schemeOptions = schemeOptions;
            _AzureADOptions = AzureADOptions;
        }

        public void Configure(string name, CookieAuthenticationOptions options)
        {
            var AzureADScheme = GetAzureADScheme(name);
            var AzureADOptions = _AzureADOptions.Get(AzureADScheme);
            if (name != AzureADOptions.CookieSchemeName)
            {
                return;
            }

            options.LoginPath = $"/AzureAD/Account/SignIn/{AzureADScheme}";
            options.LogoutPath = $"/AzureAD/Account/SignOut/{AzureADScheme}";
            options.AccessDeniedPath = "/AzureAD/Account/AccessDenied";
        }

        public void Configure(CookieAuthenticationOptions options)
        {
        }

        private string GetAzureADScheme(string name)
        {
            foreach (var mapping in _schemeOptions.Value.OpenIDMappings)
            {
                if (mapping.Value.OpenIdConnectScheme == name)
                {
                    return mapping.Key;
                }
            }

            return null;
        }
    }
}
