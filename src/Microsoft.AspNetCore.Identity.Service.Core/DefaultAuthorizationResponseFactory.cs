// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultAuthorizationResponseFactory : IAuthorizationResponseFactory
    {
        private readonly IAuthorizationResponseParameterProvider[] _providers;

        public DefaultAuthorizationResponseFactory(IEnumerable<IAuthorizationResponseParameterProvider> providers)
        {
            _providers = providers.OrderBy(p => p.Order).ToArray();
        }

        public async Task<AuthorizationResponse> CreateAuthorizationResponseAsync(TokenGeneratingContext context)
        {
            var result = new AuthorizationResponse();
            result.Message = new OpenIdConnectMessage();
            result.ResponseMode = context.RequestGrants.ResponseMode;
            result.RedirectUri = context.RequestGrants.RedirectUri;

            foreach (var provider in _providers)
            {
                await provider.AddParameters(context, result);
            }

            return result;
        }
    }
}
