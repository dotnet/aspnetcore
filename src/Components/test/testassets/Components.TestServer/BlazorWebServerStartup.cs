// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;

namespace TestServer;

public class BlazorWebServerStartup
{
    public BlazorWebServerStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Since tests run in parallel, we use an ephemeral key provider to avoid filesystem
        // contention issues.
        services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResourceRequestLog resourceRequestLog)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Mount the server-side Blazor app on /subdir
        app.Map("/subdir", app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorComponents<Root>()
                    .AddInteractiveServerRenderMode();
            });
        });
    }
}
