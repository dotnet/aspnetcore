// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal class DefaultClientRequestParametersProvider : IClientRequestParametersProvider
    {
        public DefaultClientRequestParametersProvider(
            IAbsoluteUrlFactory urlFactory,
            IOptions<ApiAuthorizationOptions> options)
        {
            UrlFactory = urlFactory;
            Options = options;
        }

        public IAbsoluteUrlFactory UrlFactory { get; }

        public IOptions<ApiAuthorizationOptions> Options { get; }

        public IDictionary<string, string> GetClientParameters(HttpContext context, string clientId)
        {
            var client = Options.Value.Clients[clientId];
            var authority = context.GetIdentityServerIssuerUri();
            var responseType = "";
            if (!client.Properties.TryGetValue(ApplicationProfilesPropertyNames.Profile, out var type))
            {
                throw new InvalidOperationException($"Can't determine the type for the client '{clientId}'");
            }

            switch (type)
            {
                case ApplicationProfiles.IdentityServerSPA:
                case ApplicationProfiles.SPA:
                case ApplicationProfiles.NativeApp:
                    responseType = "code";
                    break;
                default:
                    throw new InvalidOperationException($"Invalid application type '{type}' for '{clientId}'.");
            }

            return new Dictionary<string, string>
            {
                ["authority"] = authority,
                ["client_id"] = client.ClientId,
                ["redirect_uri"] = UrlFactory.GetAbsoluteUrl(context, client.RedirectUris.First()),
                ["post_logout_redirect_uri"] = UrlFactory.GetAbsoluteUrl(context, client.PostLogoutRedirectUris.First()),
                ["response_type"] = responseType,
                ["scope"] = string.Join(" ", client.AllowedScopes)
            };
        }
    }

}
