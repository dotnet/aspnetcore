// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using System;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    internal class JwtBearerOptionsConfiguration : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly IOptions<AzureADSchemeOptions> _schemeOptions;
        private readonly IOptionsMonitor<AzureADOptions> _azureADOptions;

        public JwtBearerOptionsConfiguration(
            IOptions<AzureADSchemeOptions> schemeOptions,
            IOptionsMonitor<AzureADOptions> azureADOptions)
        {
            _schemeOptions = schemeOptions;
            _azureADOptions = azureADOptions;
        }

        public void Configure(string name, JwtBearerOptions options)
        {
            var azureADScheme = GetAzureADScheme(name);
            var azureADOptions = _azureADOptions.Get(azureADScheme);
            if (name != azureADOptions.JwtBearerSchemeName)
            {
                return;
            }

            options.Audience = string.Format(azureADOptions.Audience?.Replace("{ClientId}", "{0}"),
                                             azureADOptions.ClientId);
            options.Authority = string.Format(azureADOptions.Authority.Replace("{Instance}", "{0}").Replace("{TenantId}", "{1}"),
                                              azureADOptions.Instance, azureADOptions.TenantId);
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
