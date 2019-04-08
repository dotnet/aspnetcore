// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityWebSite
{
    public class StartupWithGlobalAuthFilterWithoutEndpointRouting
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = BearerAuth.CreateTokenValidationParameters();
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireClaimA", policy => policy.RequireClaim("ClaimA"));
                options.AddPolicy("RequireClaimB", policy => policy.RequireClaim("ClaimB"));
            });

            services.AddMvc(o =>
            {
                o.EnableEndpointRouting = false;
                o.Filters.Add(new AuthorizeFilter("RequireClaimA"));
            })
            .AddRazorPagesOptions(options =>
            {
                options.Conventions.AllowAnonymousToPage("/AllowAnonymousPageViaConvention");
                options.Conventions.AuthorizePage("/AuthorizePageViaConvention", "RequireClaimB");
            })
            .SetCompatibilityVersion(CompatibilityVersion.Latest);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();
        }
    }
}
