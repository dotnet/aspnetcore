// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityWebSite
{
    public class StartupWithGlobalDenyAnonymousFilterWithUseMvc
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Home/Login";
                    options.LogoutPath = "/Home/Logout";
                }).AddCookie("Cookie2");

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireClaimA", policy => policy.RequireClaim("ClaimA"));
                options.AddPolicy("RequireClaimB", policy => policy.RequireClaim("ClaimB"));
            });

            services.AddMvc(o =>
            {
                o.EnableEndpointRouting = false;
                o.Filters.Add(new AuthorizeFilter());
            })
            .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddScoped<IPolicyEvaluator, CountingPolicyEvaluator>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();

            app.UseMvcWithDefaultRoute();
        }
    }
}
