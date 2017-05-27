// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Service.IntegratedWebClient
{
    public class IntegratedWebClientOpenIdConnectOptionsSetup : IConfigureNamedOptions<OpenIdConnectOptions>
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IKeySetMetadataProvider _keysProvider;
        private readonly IOptions<IntegratedWebClientOptions> _webApplicationOptions;

        public IntegratedWebClientOpenIdConnectOptionsSetup(
            IOptions<IntegratedWebClientOptions> webApplicationOptions,
            IHttpContextAccessor accessor,
            IKeySetMetadataProvider keysProvider)
        {
            _webApplicationOptions = webApplicationOptions;
            _accessor = accessor;
            _keysProvider = keysProvider;
        }

        public void Configure(string name, OpenIdConnectOptions options)
        {
            if (name != OpenIdConnectDefaults.AuthenticationScheme)
            {
                return;
            }

            options.TokenValidationParameters.NameClaimType = "name";
            options.SignInScheme = _webApplicationOptions.Value.CookieSignInScheme;
            options.ClientId = _webApplicationOptions.Value.ClientId;

            if (!string.IsNullOrEmpty(_webApplicationOptions.Value.TokenRedirectUrn))
            {
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = (ctx) =>
                    {
                        ctx.ProtocolMessage.RedirectUri = _webApplicationOptions.Value.TokenRedirectUrn;
                        return Task.CompletedTask;
                    },
                    OnRedirectToIdentityProviderForSignOut = (ctx) =>
                    {

                        ctx.ProtocolMessage.PostLogoutRedirectUri = _webApplicationOptions.Value.TokenRedirectUrn;
                        return Task.CompletedTask;
                    },
                    OnAuthorizationCodeReceived = (ctx) =>
                    {
                        ctx.ProtocolMessage.RedirectUri = _webApplicationOptions.Value.TokenRedirectUrn;
                        return Task.CompletedTask;
                    }
                };

                var keys = _keysProvider.GetKeysAsync().GetAwaiter().GetResult().Keys;

                options.ConfigurationManager = new WebApplicationConfiguration(_webApplicationOptions.Value, _accessor);
                options.TokenValidationParameters.IssuerSigningKeys = keys;
            }
        }

        public void Configure(OpenIdConnectOptions options)
        {
            Configure(OpenIdConnectDefaults.AuthenticationScheme, options);
        }
    }
}
