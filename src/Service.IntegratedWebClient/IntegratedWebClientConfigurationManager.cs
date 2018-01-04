// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service.IntegratedWebClient
{
    public class WebApplicationConfiguration : IConfigurationManager<OpenIdConnectConfiguration>
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IntegratedWebClientOptions _integratedWebClientOptions;

        private OpenIdConnectConfiguration _configuration;

        public WebApplicationConfiguration(IntegratedWebClientOptions options, IHttpContextAccessor accessor)
        {
            _integratedWebClientOptions = options;
            _accessor = accessor;
        }

        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
        {
            if (_configuration == null)
            {
                _configuration = await CreateConfigurationAsync();
            }

            return _configuration;
        }

        private async Task<OpenIdConnectConfiguration> CreateConfigurationAsync()
        {
            var ctx = _accessor.HttpContext;
            var manager = ctx.RequestServices.GetRequiredService<IConfigurationManager>();

            var configurationContext = new ConfigurationContext();
            var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host.ToUriComponent()}{ctx.Request.PathBase}";
            if (!Uri.TryCreate(configurationContext.AuthorizationEndpoint, UriKind.RelativeOrAbsolute, out var authorizationUri))
            {
                configurationContext.AuthorizationEndpoint = $"{baseUrl}/tfp/Identity/signinsignup/oauth2/v2.0/authorize";
            }
            else
            {
                configurationContext.AuthorizationEndpoint = MakeAbsolute(authorizationUri);
            }

            if (!Uri.TryCreate(configurationContext.TokenEndpoint, UriKind.RelativeOrAbsolute, out var tokenUri))
            {
                configurationContext.TokenEndpoint = $"{baseUrl}/tfp/Identity/signinsignup/oauth2/v2.0/token";
            }
            else
            {
                configurationContext.TokenEndpoint = MakeAbsolute(tokenUri);
            }

            if (!Uri.TryCreate(configurationContext.EndSessionEndpoint, UriKind.RelativeOrAbsolute, out var logoutUri))
            {
                configurationContext.EndSessionEndpoint = $"{baseUrl}/tfp/Identity/signinsignup/oauth2/v2.0/logout";
            }
            else
            {
                configurationContext.EndSessionEndpoint = MakeAbsolute(logoutUri);
            }

            configurationContext.Id = "WebApplicationClient";

            var configuration = await manager.GetConfigurationAsync(configurationContext);
            return configuration;
            string MakeAbsolute(Uri relativeOrAbsoluteUri)
            {
                if (relativeOrAbsoluteUri.IsAbsoluteUri)
                {
                    return relativeOrAbsoluteUri.ToString();
                }
                else
                {
                    return baseUrl + relativeOrAbsoluteUri.ToString();
                }
            }
        }

        public void RequestRefresh()
        {
        }
    }
}
