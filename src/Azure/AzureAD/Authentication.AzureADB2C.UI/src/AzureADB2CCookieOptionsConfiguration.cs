// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    internal class AzureADB2CCookieOptionsConfiguration : IConfigureNamedOptions<CookieAuthenticationOptions>
    {
        private readonly IOptions<AzureADB2CSchemeOptions> _schemeOptions;
        private readonly IOptionsMonitor<AzureADB2COptions> _azureADB2COptions;

        public AzureADB2CCookieOptionsConfiguration(IOptions<AzureADB2CSchemeOptions> schemeOptions, IOptionsMonitor<AzureADB2COptions> azureADB2COptions)
        {
            _schemeOptions = schemeOptions;
            _azureADB2COptions = azureADB2COptions;
        }

        public void Configure(string name, CookieAuthenticationOptions options)
        {
            var azureADB2CScheme = GetAzureADB2CScheme(name);
            var azureADB2COptions = _azureADB2COptions.Get(azureADB2CScheme);
            if (name != azureADB2COptions.CookieSchemeName)
            {
                return;
            }

            options.LoginPath = $"/AzureADB2C/Account/SignIn/{azureADB2CScheme}";
            options.LogoutPath = $"/AzureADB2C/Account/SignOut/{azureADB2CScheme}";
            options.AccessDeniedPath = "/AzureADB2C/Account/AccessDenied";
            options.Cookie.SameSite = SameSiteMode.None;
        }

        public void Configure(CookieAuthenticationOptions options)
        {
        }

        private string GetAzureADB2CScheme(string name)
        {
            foreach (var mapping in _schemeOptions.Value.OpenIDMappings)
            {
                if (mapping.Value.CookieScheme == name)
                {
                    return mapping.Key;
                }
            }

            return null;
        }
    }
}
