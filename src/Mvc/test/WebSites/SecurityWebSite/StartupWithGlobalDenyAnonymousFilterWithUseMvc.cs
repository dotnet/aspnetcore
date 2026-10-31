// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace SecurityWebSite;

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
        });

        services.AddScoped<IPolicyEvaluator, CountingPolicyEvaluator>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseAuthentication();

        app.UseMvcWithDefaultRoute();
    }
}
