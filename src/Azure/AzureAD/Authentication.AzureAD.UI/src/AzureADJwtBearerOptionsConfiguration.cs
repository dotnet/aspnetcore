// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    [Obsolete("This is obsolete and will be removed in a future version. Use Microsoft.Identity.Web instead. See https://aka.ms/ms-identity-web.")]
    internal class AzureADJwtBearerOptionsConfiguration : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly IOptions<AzureADSchemeOptions> _schemeOptions;
        private readonly IOptionsMonitor<AzureADOptions> _azureADOptions;

        public AzureADJwtBearerOptionsConfiguration(
            IOptions<AzureADSchemeOptions> schemeOptions,
            IOptionsMonitor<AzureADOptions> azureADOptions)
        {
            _schemeOptions = schemeOptions;
            _azureADOptions = azureADOptions;
        }

        public void Configure(string name, JwtBearerOptions options)
        {
            var azureADScheme = GetAzureADScheme(name);
            if (azureADScheme is null)
            {
                return;
            }

            var azureADOptions = _azureADOptions.Get(azureADScheme);
            if (name != azureADOptions.JwtBearerSchemeName)
            {
                return;
            }

            options.Audience = azureADOptions.ClientId;
            options.Authority = new Uri(new Uri(azureADOptions.Instance), azureADOptions.TenantId).ToString();
        }

        public void Configure(JwtBearerOptions options)
        {
        }

        private string GetAzureADScheme(string name)
        {
            foreach (var mapping in _schemeOptions.Value.JwtBearerMappings)
            {
                if (mapping.Value.JwtBearerScheme == name)
                {
                    return mapping.Key;
                }
            }

            return null;
        }
    }
}
