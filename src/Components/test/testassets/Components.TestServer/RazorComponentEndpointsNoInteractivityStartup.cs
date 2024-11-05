// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Web;
using Components.TestServer.RazorComponents;
using Components.TestServer.RazorComponents.Pages.Forms;
using Components.TestServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace TestServer;

public class RazorComponentEndpointsNoInteractivityStartup<TRootComponent>
{
    public RazorComponentEndpointsNoInteractivityStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorComponents(options =>
        {
            options.MaxFormMappingErrorCount = 10;
            options.MaxFormMappingRecursionDepth = 5;
            options.MaxFormMappingCollectionSize = 100;
        });
        services.AddHttpContextAccessor();
        services.AddCascadingAuthenticationState();
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
            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
            }

            app.UseStaticFiles();
            app.UseRouting();
            RazorComponentEndpointsStartup<TRootComponent>.UseFakeAuthState(app);
            app.UseAntiforgery();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorComponents<TRootComponent>();
            });
        });
    }
}
