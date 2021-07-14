// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Duende.IdentityServer.Models;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration
{
    internal class ConfigureApiScopes : IPostConfigureOptions<ApiAuthorizationOptions>
    {
        public void PostConfigure(string name, ApiAuthorizationOptions options)
        {
            AddResourceScopesToApiScopes(options);
        }

        private void AddResourceScopesToApiScopes(ApiAuthorizationOptions options)
        {
            foreach (var resource in options.ApiResources)
            {
                foreach (var scope in resource.Scopes)
                {
                    if (!options.ApiScopes.ContainsScope(scope))
                    {
                        options.ApiScopes.Add(new ApiScope(scope));
                    }
                }
            }
        }
    }
}
