// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Duende.IdentityServer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal class ConfigureClients : IConfigureOptions<ApiAuthorizationOptions>
    {
        private const string DefaultLocalSPARelativeRedirectUri = "/authentication/login-callback";
        private const string DefaultLocalSPARelativePostLogoutRedirectUri = "/authentication/logout-callback";

        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigureClients> _logger;

        public ConfigureClients(
            IConfiguration configuration,
            ILogger<ConfigureClients> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Configure(ApiAuthorizationOptions options)
        {
            foreach (var client in GetClients())
            {
                options.Clients.Add(client);
            }
        }

        internal IEnumerable<Client> GetClients()
        {
            var data = _configuration.Get<Dictionary<string, ClientDefinition>>();
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    _logger.LogInformation(LoggerEventIds.ConfiguringClient, "Configuring client '{ClientName}'.", kvp.Key);
                    var name = kvp.Key;
                    var definition = kvp.Value;

                    switch (definition.Profile)
                    {
                        case ApplicationProfiles.SPA:
                            yield return GetSPA(name, definition);
                            break;
                        case ApplicationProfiles.IdentityServerSPA:
                            yield return GetLocalSPA(name, definition);
                            break;
                        case ApplicationProfiles.NativeApp:
                            yield return GetNativeApp(name, definition);
                            break;
                        default:
                            throw new InvalidOperationException($"Type '{definition.Profile}' is not supported.");
                    }
                }
            }
        }

        private Client GetSPA(string name, ClientDefinition definition)
        {
            if (definition.RedirectUri == null ||
                !Uri.TryCreate(definition.RedirectUri, UriKind.Absolute, out var redirectUri))
            {
                throw new InvalidOperationException($"The redirect uri " +
                    $"'{definition.RedirectUri}' for '{name}' is invalid. " +
                    $"The redirect URI must be an absolute url.");
            }

            if (definition.LogoutUri == null ||
                !Uri.TryCreate(definition.LogoutUri, UriKind.Absolute, out var postLogouturi))
            {
                throw new InvalidOperationException($"The logout uri " +
                    $"'{definition.LogoutUri}' for '{name}' is invalid. " +
                    $"The logout URI must be an absolute url.");
            }

            if (!string.Equals(
                redirectUri.GetLeftPart(UriPartial.Authority),
                postLogouturi.GetLeftPart(UriPartial.Authority),
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"The redirect uri and the logout uri " +
                    $"for '{name}' have a different scheme, host or port.");
            }

            var client = ClientBuilder.SPA(name)
                .WithRedirectUri(definition.RedirectUri)
                .WithLogoutRedirectUri(definition.LogoutUri)
                .WithAllowedOrigins(redirectUri.GetLeftPart(UriPartial.Authority))
                .FromConfiguration();

            return client.Build();
        }

        private Client GetNativeApp(string name, ClientDefinition definition)
        {
            var client = ClientBuilder.NativeApp(name)
                .FromConfiguration();
            return client.Build();
        }

        private Client GetLocalSPA(string name, ClientDefinition definition)
        {
            var client = ClientBuilder
                .IdentityServerSPA(name)
                .WithRedirectUri(definition.RedirectUri ?? DefaultLocalSPARelativeRedirectUri)
                .WithLogoutRedirectUri(definition.LogoutUri ?? DefaultLocalSPARelativePostLogoutRedirectUri)
                .WithAllowedOrigins()
                .FromConfiguration();

            return client.Build();
        }
    }
}
