// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
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
}
