// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal class IdentityServerJwtBearerOptionsConfiguration : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly string _scheme;
        private readonly string _apiName;
        private readonly IIdentityServerJwtDescriptor _localApiDescriptor;

        public IdentityServerJwtBearerOptionsConfiguration(
            string scheme,
            string apiName,
            IIdentityServerJwtDescriptor localApiDescriptor)
        {
            _scheme = scheme;
            _apiName = apiName;
            _localApiDescriptor = localApiDescriptor;
        }

        public void Configure(string name, JwtBearerOptions options)
        {
            var definitions = _localApiDescriptor.GetResourceDefinitions();
            if (!definitions.ContainsKey(_apiName))
            {
                return;
            }

            if (string.Equals(name, _scheme, StringComparison.Ordinal))
            {
                options.Events = options.Events ?? new JwtBearerEvents();
                options.Events.OnMessageReceived = ResolveAuthorityAndKeysAsync;
                options.Audience = _apiName;

                var staticConfiguration = new OpenIdConnectConfiguration
                {
                    Issuer = options.Authority
                };

                var manager = new StaticConfigurationManager(staticConfiguration);
                options.ConfigurationManager = manager;
                options.TokenValidationParameters.ValidIssuer = options.Authority;
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";
            }
        }

        internal static async Task ResolveAuthorityAndKeysAsync(MessageReceivedContext messageReceivedContext)
        {
            var options = messageReceivedContext.Options;
            if (options.TokenValidationParameters.ValidIssuer == null || options.TokenValidationParameters.IssuerSigningKey == null)
            {
                var store = messageReceivedContext.HttpContext.RequestServices.GetRequiredService<ISigningCredentialStore>();
                var credential = await store.GetSigningCredentialsAsync();
                options.Authority = options.Authority ?? messageReceivedContext.HttpContext.GetIdentityServerIssuerUri();
                options.TokenValidationParameters.IssuerSigningKey = credential.Key;
                options.TokenValidationParameters.ValidIssuer = options.Authority;
            }
        }

        public void Configure(JwtBearerOptions options)
        {
        }
    }
}
