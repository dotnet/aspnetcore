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
            app.Map("/reexecution", reexecutionApp =>
            {
                app.Map("/trigger-404", trigger404App =>
                {
                    trigger404App.Run(async context =>
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Triggered a 404 status code.");
                    });
                });

                if (!env.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error", createScopeForErrors: true);
                }

                reexecutionApp.UseStatusCodePagesWithReExecute("/not-found-reexecute", createScopeForErrors: true);
                reexecutionApp.UseStaticFiles();
                reexecutionApp.UseRouting();
                RazorComponentEndpointsStartup<TRootComponent>.UseFakeAuthState(reexecutionApp);
                reexecutionApp.UseAntiforgery();
                reexecutionApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorComponents<TRootComponent>()
                        .AddAdditionalAssemblies(Assembly.Load("TestContentPackage"));
                });
            });

            ConfigureSubdirPipeline(app, env);
        });
    }

    private void ConfigureSubdirPipeline(IApplicationBuilder app, IWebHostEnvironment env)
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
            endpoints.MapRazorComponents<TRootComponent>()
                .AddAdditionalAssemblies(Assembly.Load("TestContentPackage"));
        });
    }
}
