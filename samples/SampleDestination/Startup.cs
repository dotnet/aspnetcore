// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SampleDestination
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors(policy => policy
                .WithOrigins("http://origin.example.com:5001")
                .WithMethods("PUT")
                .WithHeaders("Cache-Control"));

            app.Run(async context =>
            {
                var responseHeaders = context.Response.Headers;
                context.Response.ContentType = "text/plain";
                foreach (var responseHeader in responseHeaders)
                {
                    await context.Response.WriteAsync("\n" + responseHeader.Key + ": " + responseHeader.Value);
                }

                await context.Response.WriteAsync("\nStatus code of your request: " + context.Response.StatusCode.ToString());
            });
        }
    }
}
