// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
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
                options.MaxJSRootComponents = 5; // To make it easier to test
            });
            services.AddSingleton<ResourceRequestLog>();

            // Since tests run in parallel, we use an ephemeral key provider to avoid filesystem
            // contention issues.
            services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResourceRequestLog resourceRequestLog)
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
                    endpoints.MapBlazorHub().WithJSComponents(rootComponents =>
                    {
                        rootComponents.RegisterForJavaScript<BasicTestApp.DynamicallyAddedRootComponent>("my-dynamic-root-component");
                    });

                    endpoints.MapControllerRoute("mvc", "{controller}/{action}");
                    endpoints.MapFallbackToPage("/_ServerHost");
                });
            });
        }
    }
}
