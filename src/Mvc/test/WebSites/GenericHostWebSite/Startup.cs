// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace GenericHostWebSite;

public class Startup
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new TestGenericService { Message = "true" });

        services
            .AddControllers(options =>
            {
                // Remove when all URL generation tests are passing - https://github.com/aspnet/Routing/issues/590
                options.EnableEndpointRouting = false;
            });

        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddScoped<TestResponseGenerator>();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();

        app.UseStaticFiles();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                "areaRoute",
                "{area:exists}/{controller}/{action}",
                new { controller = "Home", action = "Index" });

            endpoints.MapControllerRoute("ActionAsMethod", "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" });

            endpoints.MapControllerRoute("PageRoute", "{controller}/{action}/{page}");
        });
    }
}
