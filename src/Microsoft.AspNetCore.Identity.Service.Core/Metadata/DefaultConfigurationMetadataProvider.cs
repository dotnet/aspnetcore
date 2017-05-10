// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service.Metadata
{
    public class DefaultConfigurationMetadataProvider : IConfigurationMetadataProvider
    {
        private readonly IOptions<IdentityServiceOptions> _options;

        public DefaultConfigurationMetadataProvider(IOptions<IdentityServiceOptions> options)
        {
            _options = options;
        }

        public int Order { get; } = 1000;

        public Task ConfigureMetadataAsync(OpenIdConnectConfiguration configuration, ConfigurationContext context)
        {
            configuration.Issuer = _options.Value.Issuer;
            configuration.AuthorizationEndpoint = context.AuthorizationEndpoint;
            configuration.TokenEndpoint = context.TokenEndpoint;
            configuration.JwksUri = context.JwksUriEndpoint;
            configuration.EndSessionEndpoint = context.EndSessionEndpoint;
            configuration.ResponseModesSupported.Add(OpenIdConnectResponseMode.Query);
            configuration.ResponseModesSupported.Add(OpenIdConnectResponseMode.Fragment);
            configuration.ResponseModesSupported.Add(OpenIdConnectResponseMode.FormPost);
            configuration.ResponseTypesSupported.Add(OpenIdConnectResponseType.Code);
            configuration.ResponseTypesSupported.Add(OpenIdConnectResponseType.IdToken);
            configuration.ResponseTypesSupported.Add(OpenIdConnectResponseType.CodeIdToken);
            configuration.ScopesSupported.Add("openid");
            configuration.SubjectTypesSupported.Add("pairwise");
            configuration.IdTokenSigningAlgValuesSupported.Add("RS256");
            configuration.TokenEndpointAuthMethodsSupported.Add("client_secret_post");
            configuration.ClaimsSupported.Add("oid");
            configuration.ClaimsSupported.Add("sub");
            configuration.ClaimsSupported.Add("name");

            return Task.CompletedTask;
        }
    }
}
