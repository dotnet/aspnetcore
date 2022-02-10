// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;

namespace TestServer;

public class ServerStartup
{
    public ServerStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc();
        services.AddServerSideBlazor(options =>
        {
            options.RootComponents.MaxJSRootComponents = 5; // To make it easier to test
            options.RootComponents.RegisterForJavaScript<BasicTestApp.DynamicallyAddedRootComponent>("my-dynamic-root-component");
            options.RootComponents.RegisterForJavaScript<BasicTestApp.JavaScriptRootComponentParameterTypes>(
                "component-with-many-parameters",
                javaScriptInitializer: "myJsRootComponentInitializers.testInitializer");
        });
        services.AddSingleton<ResourceRequestLog>();
        services.AddTransient<BasicTestApp.FormsTest.ValidationComponentDI.SaladChef>();

        // Since tests run in parallel, we use an ephemeral key provider to avoid filesystem
        // contention issues.
        services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResourceRequestLog resourceRequestLog)
    {
        var enUs = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = enUs;
        CultureInfo.DefaultThreadCurrentUICulture = enUs;

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Mount the server-side Blazor app on /subdir
        app.Map("/subdir", app =>
        {
            app.Use((context, next) =>
            {
                if (context.Request.Path.Value.EndsWith("/images/blazor_logo_1000x.png", StringComparison.Ordinal))
                {
                    resourceRequestLog.AddRequest(context.Request);
                }

                return next(context);
            });

            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapControllerRoute("mvc", "{controller}/{action}");
                endpoints.MapFallbackToPage("/_ServerHost");
            });
        });
    }
}
