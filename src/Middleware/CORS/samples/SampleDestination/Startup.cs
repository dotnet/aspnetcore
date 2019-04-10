// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SampleDestination
{
    public class Startup
    {
        private static readonly string DefaultAllowedOrigin = $"http://{Dns.GetHostName()}:9001";
        private readonly ILogger<Startup> _logger;

        public Startup(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Startup>();
            _logger.LogInformation($"Setting up CORS middleware to allow clients on {DefaultAllowedOrigin}");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Map("/allow-origin", innerBuilder =>
            {
                innerBuilder.UseCors(policy => policy
                    .WithOrigins(DefaultAllowedOrigin)
                    .AllowAnyMethod()
                    .AllowAnyHeader());

                innerBuilder.UseMiddleware<SampleMiddleware>();
            });

            app.Map("/allow-header-method", innerBuilder =>
            {
                innerBuilder.UseCors(policy => policy
                    .WithOrigins(DefaultAllowedOrigin)
                    .WithHeaders("X-Test", "Content-Type")
                    .WithMethods("PUT"));

                innerBuilder.UseMiddleware<SampleMiddleware>();
            });

            app.Map("/allow-credentials", innerBuilder =>
            {
                innerBuilder.UseCors(policy => policy
                    .WithOrigins(DefaultAllowedOrigin)
                    .AllowAnyHeader()
                    .WithMethods("GET", "PUT")
                    .AllowCredentials());

                innerBuilder.UseMiddleware<SampleMiddleware>();
            });

            app.Map("/exposed-header", innerBuilder =>
            {
                innerBuilder.UseCors(policy => policy
                    .WithOrigins(DefaultAllowedOrigin)
                    .WithExposedHeaders("X-AllowedHeader", "Content-Length"));

                innerBuilder.UseMiddleware<SampleMiddleware>();
            });

            app.Map("/allow-all", innerBuilder =>
            {
                innerBuilder.UseCors(policy => policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());

                innerBuilder.UseMiddleware<SampleMiddleware>();
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
