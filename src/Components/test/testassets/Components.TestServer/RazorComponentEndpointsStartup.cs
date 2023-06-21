// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Components.TestServer.RazorComponents;
using Components.TestServer.RazorComponents.Pages;
using Components.TestServer.RazorComponents.Pages.Forms;
using Components.TestServer.Services;
using Microsoft.AspNetCore.Components;

namespace TestServer;

public class RazorComponentEndpointsStartup<TRootComponent>
{
    public RazorComponentEndpointsStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddServerComponents()
            .AddWebAssemblyComponents(options =>
            {
                options.PathPrefix = "/WasmMinimal";
            });
        services.AddHttpContextAccessor();
        services.AddSingleton<AsyncOperationService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var enUs = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = enUs;
        CultureInfo.DefaultThreadCurrentUICulture = enUs;

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.Map("/subdir", app =>
        {
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorComponents<TRootComponent>()
                    .AddServerRenderMode()
                    .AddWebAssemblyRenderMode();

                NotEnabledStreamingRenderingComponent.MapEndpoints(endpoints);
                StreamingRenderingForm.MapEndpoints(endpoints);

                MapEnhancedNavigationEndpoints(endpoints);
            });
        });
    }

    private static void MapEnhancedNavigationEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Used when testing that enhanced nav can show non-HTML responses (which it does by doing a full navigation)
        endpoints.Map("/nav/non-html-response", () => "Hello, this is plain text");

        // Used when testing that enhanced nav displays content even if the response is an error status code
        endpoints.Map("/nav/give-404-with-content", async (HttpResponse response) =>
        {
            response.StatusCode = 404;
            response.ContentType = "text/html";
            await response.WriteAsync("<h1>404</h1><p>Sorry, there's nothing here! This is a custom server-generated 404 message.</p>");
        });
    }
}
