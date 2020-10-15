// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
    public class CorsStartup
    {
        public CorsStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddMvc();
            services.AddCors(options =>
            {
                // It's not enough just to return "Access-Control-Allow-Origin: *", because
                // browsers don't allow wildcards in conjunction with credentials. So we must
                // specify explicitly which origin we want to allow.

                options.AddPolicy("AllowAll", policy => policy
                    .SetIsOriginAllowed(host => host.StartsWith("http://localhost:", StringComparison.Ordinal) || host.StartsWith("http://127.0.0.1:", StringComparison.Ordinal))
                    .AllowAnyHeader()
                    .WithExposedHeaders("MyCustomHeader")
                    .AllowAnyMethod()
                    .AllowCredentials());
            });
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

            // Mount the server-side Blazor app on /subdir
            app.Map("/subdir", app =>
            {
                app.UseBlazorFrameworkFiles();
                app.UseStaticFiles();

                app.UseRouting();

                app.UseCors("AllowAll");

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<ChatHub>("/chathub");
                    endpoints.MapControllers();
                    endpoints.MapFallbackToFile("index.html");
                });
            });
        }
    }
}
