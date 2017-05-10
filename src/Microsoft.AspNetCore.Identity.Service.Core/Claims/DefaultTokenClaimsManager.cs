// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Service.Claims;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultTokenClaimsManager : ITokenClaimsManager
    {
        private readonly ITokenClaimsProvider[] _providers;

        public DefaultTokenClaimsManager(IEnumerable<ITokenClaimsProvider> providers)
        {
            _providers = providers.OrderBy(p => p.Order).ToArray();
        }

        public async Task CreateClaimsAsync(TokenGeneratingContext context)
        {
            foreach (var provider in _providers)
            {
                await provider.OnGeneratingClaims(context);
            }
        }
    }
}
