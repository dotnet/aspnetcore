// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BasicWebSite;

public class Startup
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc()
            .AddNewtonsoftJson()
            .AddXmlDataContractSerializerFormatters();

        services.ConfigureBaseWebSiteAuthPolicies();

        services.AddHttpContextAccessor();
        services.AddScoped<RequestIdService>();
        services.AddScoped<TestResponseGenerator>();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();

        // Initializes the RequestId service for each request
        app.UseMiddleware<RequestIdMiddleware>();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "ActionAsMethod",
                pattern: "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" });

            endpoints.MapControllerRoute(
                name: "PageRoute",
                pattern: "{controller}/{action}/{page}");
        });
    }
}
