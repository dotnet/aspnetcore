// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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
        private static readonly string DefaultAllowedOrigin = $"http://{Dns.GetHostName()}:{Environment.GetEnvironmentVariable("ORIGIN_PORT")}";
        private readonly ILogger<Startup> _logger;

        public Startup(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Startup>();
            _logger.LogInformation($"Setting up CORS middleware to allow clients on {DefaultAllowedOrigin}");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowOrigin", policy => policy
                    .WithOrigins(DefaultAllowedOrigin)
                    .AllowAnyMethod()
                    .AllowAnyHeader());

                options.AddPolicy("AllowHeaderMethod", policy => policy
                    .WithOrigins(DefaultAllowedOrigin)
                    .WithHeaders("X-Test", "Content-Type")
                    .WithMethods("PUT"));

                options.AddPolicy("AllowCredentials", policy => policy
                    .WithOrigins(DefaultAllowedOrigin)
                    .AllowAnyHeader()
                    .WithMethods("GET", "PUT")
                    .AllowCredentials());

                options.AddPolicy("ExposedHeader", policy => policy
                    .WithOrigins(DefaultAllowedOrigin)
                    .WithExposedHeaders("X-AllowedHeader", "Content-Length"));

                options.AddPolicy("AllowAll", policy => policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseCors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/allow-origin", HandleRequest).RequireCors("AllowOrigin");
                endpoints.Map("/allow-header-method", HandleRequest).RequireCors("AllowHeaderMethod");
                endpoints.Map("/allow-credentials", HandleRequest).RequireCors("AllowCredentials");
                endpoints.Map("/exposed-header", HandleRequest).RequireCors("ExposedHeader");
                endpoints.Map("/allow-all", HandleRequest).RequireCors("AllowAll");
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        private Task HandleRequest(HttpContext context)
        {
            var content = Encoding.UTF8.GetBytes("Hello world");

            context.Response.Headers["X-AllowedHeader"] = "Test-Value";
            context.Response.Headers["X-DisallowedHeader"] = "Test-Value";

            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength = content.Length;
            return context.Response.Body.WriteAsync(content, 0, content.Length);
        }
    }
}
