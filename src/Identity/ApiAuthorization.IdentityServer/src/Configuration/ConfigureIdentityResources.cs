// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal class ConfigureIdentityResources : IConfigureOptions<ApiAuthorizationOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigureIdentityResources> _logger;
        private static readonly char[] ScopesSeparator = new char[] { ' ' };

        public ConfigureIdentityResources(IConfiguration configuration, ILogger<ConfigureIdentityResources> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Configure(ApiAuthorizationOptions options)
        {
            var data = _configuration.Get<IdentityResourceDefinition>();
            if (data != null && data.Scopes != null)
            {
                var scopes = ParseScopes(data.Scopes);
                if (scopes != null && scopes.Length > 0)
                {
                    ClearDefaultIdentityResources(options);
                }
                foreach (var scope in scopes)
                {
                    switch (scope)
                    {
                        case Duende.IdentityServer.IdentityServerConstants.StandardScopes.OpenId:
                            options.IdentityResources.Add(IdentityResourceBuilder.OpenId()
                                .AllowAllClients()
                                .FromConfiguration()
                                .Build());
                            break;
                        case Duende.IdentityServer.IdentityServerConstants.StandardScopes.Profile:
                            options.IdentityResources.Add(IdentityResourceBuilder.Profile()
                                .AllowAllClients()
                                .FromConfiguration()
                                .Build());
                            break;
                        case Duende.IdentityServer.IdentityServerConstants.StandardScopes.Address:
                            options.IdentityResources.Add(IdentityResourceBuilder.Address()
                                .AllowAllClients()
                                .FromConfiguration()
                                .Build());
                            break;
                        case Duende.IdentityServer.IdentityServerConstants.StandardScopes.Email:
                            options.IdentityResources.Add(IdentityResourceBuilder.Email()
                                .AllowAllClients()
                                .FromConfiguration()
                                .Build());
                            break;
                        case Duende.IdentityServer.IdentityServerConstants.StandardScopes.Phone:
                            options.IdentityResources.Add(IdentityResourceBuilder.Phone()
                                .AllowAllClients()
                                .FromConfiguration()
                                .Build());
                            break;
                        default:
                            throw new InvalidOperationException($"Invalid identity resource name '{scope}'");
                    }
                }
            }
        }

        private static void ClearDefaultIdentityResources(ApiAuthorizationOptions options)
        {
            var allDefault = true;
            foreach (var resource in options.IdentityResources)
            {
                if (!resource.Properties.TryGetValue(ApplicationProfilesPropertyNames.Source, out var source) ||
                    !string.Equals(ApplicationProfilesPropertyValues.Default, source, StringComparison.OrdinalIgnoreCase))
                {
                    allDefault = false;
                    break;
                }
            }
            if (allDefault)
            {
                options.IdentityResources.Clear();
            }
        }

        private string[] ParseScopes(string scopes)
        {
            if (scopes == null)
            {
                return null;
            }

            var parsed = scopes.Split(ScopesSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (parsed.Length == 0)
            {
                return null;
            }

            return parsed;
        }
    }
}
