// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ComponentsApp.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorComponents<App.Startup>();
            services.AddSingleton<WeatherForecastService, DefaultWeatherForecastService>();
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.Use((ctx, next) =>
            {
                // When we're prerendering, we have to initialize the URI helper because
                // BlazorHub won't exist to do it for us
                var isPrerendering = ctx.WebSockets?.IsWebSocketRequest == false;
                if (isPrerendering)
                {
                    var request = ctx.Request;
                    var uriHelper = (RemoteUriHelper)ctx.RequestServices.GetRequiredService<IUriHelper>();
                    uriHelper.Initialize(
                        $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}",
                        $"{request.Scheme}://{request.Host}{request.PathBase}/");
                }
                return next();
            });

            app.UseStaticFiles();
            app.UseRazorComponents<App.Startup>();
            app.UseMvc();
        }
    }
}
