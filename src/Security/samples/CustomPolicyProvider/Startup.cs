// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CustomPolicyProvider;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Replace the default authorization policy provider with our own
        // custom provider which can return authorization policies for given
        // policy names (instead of using the default policy provider)
        services.AddSingleton<IAuthorizationPolicyProvider, MinimumAgePolicyProvider>();

        // As always, handlers must be provided for the requirements of the authorization policies
        services.AddSingleton<IAuthorizationHandler, MinimumAgeAuthorizationHandler>();

        services.AddMvc();

        // Add cookie authentication so that it's possible to sign-in to test the
        // custom authorization policy behavior of the sample
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.AccessDeniedPath = "/account/denied";
                options.LoginPath = "/account/signin";
            });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }
}
