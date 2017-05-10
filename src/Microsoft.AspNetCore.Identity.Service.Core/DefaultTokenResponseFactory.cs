// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultTokenResponseFactory : ITokenResponseFactory
    {
        private readonly ITokenResponseParameterProvider[] _providers;

        public DefaultTokenResponseFactory(IEnumerable<ITokenResponseParameterProvider> providers)
        {
            _providers = providers.OrderBy(o => o.Order).ToArray();
        }

        public async Task<OpenIdConnectMessage> CreateTokenResponseAsync(TokenGeneratingContext context)
        {
            var response = new OpenIdConnectMessage();
            foreach (var provider in _providers)
            {
                await provider.AddParameters(context, response);
            }

            return response;
        }
    }
}
