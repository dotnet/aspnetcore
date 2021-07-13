// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Duende.IdentityServer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration
{
    internal class ConfigureClientScopes : IPostConfigureOptions<ApiAuthorizationOptions>
    {
        private static readonly char[] DefaultClientListSeparator = new char[] { ' ' };
        private readonly ILogger<ConfigureClientScopes> _logger;

        public ConfigureClientScopes(ILogger<ConfigureClientScopes> logger)
        {
            _logger = logger;
        }

        public void PostConfigure(string name, ApiAuthorizationOptions options)
        {
            AddApiResourceScopesToClients(options);
            AddIdentityResourceScopesToClients(options);
        }

        private void AddIdentityResourceScopesToClients(ApiAuthorizationOptions options)
        {
            foreach (var identityResource in options.IdentityResources)
            {
                if (!identityResource.Properties.TryGetValue(ApplicationProfilesPropertyNames.Clients, out var clientList))
                {
                    _logger.LogInformation(LoggerEventIds.AllowedApplicationNotDefienedForIdentityResource, "Identity resource '{IdentityResourceName}' doesn't define a list of allowed applications.", identityResource.Name);
                    continue;
                }

                var resourceClients = clientList.Split(DefaultClientListSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (resourceClients.Length == 0)
                {
                    _logger.LogInformation(LoggerEventIds.AllowedApplicationNotDefienedForIdentityResource, "Identity resource '{IdentityResourceName}' doesn't define a list of allowed applications.", identityResource.Name);
                    continue;
                }

                if (resourceClients.Length == 1 && resourceClients[0] == ApplicationProfilesPropertyValues.AllowAllApplications)
                {
                    _logger.LogInformation(LoggerEventIds.AllApplicationsAllowedForIdentityResource, "Identity resource '{IdentityResourceName}' allows all applications.", identityResource.Name);
                }
                else
                {
                    _logger.LogInformation(LoggerEventIds.ApplicationsAllowedForIdentityResource, "Identity resource '{IdentityResourceName}' allows applications '{ResourceClients}'.", identityResource.Name, string.Join(" ", resourceClients));
                }

                foreach (var client in options.Clients)
                {
                    if ((resourceClients.Length == 1 && resourceClients[0] == ApplicationProfilesPropertyValues.AllowAllApplications) ||
                        resourceClients.Contains(client.ClientId))
                    {
                        client.AllowedScopes.Add(identityResource.Name);
                    }
                }
            }
        }

        private void AddApiResourceScopesToClients(ApiAuthorizationOptions options)
        {
            foreach (var resource in options.ApiResources)
            {
                if (!resource.Properties.TryGetValue(ApplicationProfilesPropertyNames.Clients, out var clientList))
                {
                    _logger.LogInformation(LoggerEventIds.AllowedApplicationNotDefienedForApiResource, "Resource '{ApiResourceName}' doesn't define a list of allowed applications.", resource.Name);
                    continue;
                }

                var resourceClients = clientList.Split(DefaultClientListSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (resourceClients.Length == 0)
                {
                    _logger.LogInformation(LoggerEventIds.AllowedApplicationNotDefienedForApiResource, "Resource '{ApiResourceName}' doesn't define a list of allowed applications.", resource.Name);
                    continue;
                }

                if (resourceClients.Length == 1 && resourceClients[0] == ApplicationProfilesPropertyValues.AllowAllApplications)
                {
                    _logger.LogInformation(LoggerEventIds.AllApplicationsAllowedForApiResource, "Resource '{ApiResourceName}' allows all applications.", resource.Name);
                }
                else
                {
                    _logger.LogInformation(LoggerEventIds.ApplicationsAllowedForApiResource, "Resource '{ApiResourceName}' allows applications '{resourceClients}'.", resource.Name, string.Join(" ", resourceClients));
                }

                foreach (var client in options.Clients)
                {
                    if ((resourceClients.Length == 1 && resourceClients[0] == ApplicationProfilesPropertyValues.AllowAllApplications) ||
                        resourceClients.Contains(client.ClientId))
                    {
                        AddScopes(resource, client);
                    }
                }
            }
        }

        private static void AddScopes(ApiResource resource, Client client)
        {
            foreach (var scope in resource.Scopes)
            {
                client.AllowedScopes.Add(scope);
            }
        }
    }
}
