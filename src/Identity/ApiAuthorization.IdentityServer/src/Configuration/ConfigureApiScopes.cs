// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Duende.IdentityServer.Models;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;

internal sealed class ConfigureApiScopes : IPostConfigureOptions<ApiAuthorizationOptions>
{
    public void PostConfigure(string name, ApiAuthorizationOptions options)
    {
        AddResourceScopesToApiScopes(options);
    }

    private static void AddResourceScopesToApiScopes(ApiAuthorizationOptions options)
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
