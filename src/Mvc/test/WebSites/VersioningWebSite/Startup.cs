// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace VersioningWebSite;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(ConfigureMvcOptions)
            .AddNewtonsoftJson();

        services.AddScoped<TestResponseGenerator>();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }

    protected virtual void ConfigureMvcOptions(MvcOptions options)
    {
    }
}
