// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicWebSite;

public class StartupWithSessionTempDataProvider
{
    public void ConfigureServices(IServiceCollection services)
    {
        // CookieTempDataProvider is the default ITempDataProvider, so we must override it with session.
        services
            .AddMvc()
            .AddSessionStateTempDataProvider();
        services.AddSession();

        services.ConfigureBaseWebSiteAuthPolicies();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseSession();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapRazorPages();
        });
    }
}

