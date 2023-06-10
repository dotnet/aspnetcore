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
        services.AddRazorComponents();
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
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorComponents<TRootComponent>();

                StreamingRendering.MapEndpoints(endpoints);
                StreamingRenderingForm.MapEndpoints(endpoints);
            });
        });
    }
}
