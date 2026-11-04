// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace BasicWebSite;

internal static class ConfigureAuthPoliciesExtensions
{
    public static void ConfigureBaseWebSiteAuthPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // This policy cannot succeed since the claim is never added
            options.AddPolicy("Impossible", policy =>
            {
                policy.AuthenticationSchemes.Add("Api");
                policy.RequireClaim("Never");
            });
            options.AddPolicy("Api", policy =>
            {
                policy.AuthenticationSchemes.Add("Api");
                policy.RequireClaim(ClaimTypes.NameIdentifier);
            });
            options.AddPolicy("Api-Manager", policy =>
            {
                policy.AuthenticationSchemes.Add("Api");
                policy.Requirements.Add(Operations.Edit);
            });
        });
    }
}
