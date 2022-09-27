// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server;

internal sealed class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
    }

    public static void Configure(IApplicationBuilder app, IConfiguration configuration)
    {
        app.UseDeveloperExceptionPage();
        EnableConfiguredPathbase(app, configuration);

        app.UseWebAssemblyDebugging();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            // In development, serve everything, as there's no other way to configure it.
            // In production, developers are responsible for configuring their own production server
            ServeUnknownFileTypes = true,
        });

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapFallbackToFile("index.html");
        });
    }

    private static void EnableConfiguredPathbase(IApplicationBuilder app, IConfiguration configuration)
    {
        var pathBase = configuration.GetValue<string>("pathbase");
        if (!string.IsNullOrEmpty(pathBase))
        {
            app.UsePathBase(pathBase);

            // To ensure consistency with a production environment, only handle requests
            // that match the specified pathbase.
            app.Use((context, next) =>
            {
                if (context.Request.PathBase == pathBase)
                {
                    return next(context);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    return context.Response.WriteAsync($"The server is configured only to " +
                        $"handle request URIs within the PathBase '{pathBase}'.");
                }
            });
        }
    }
}
