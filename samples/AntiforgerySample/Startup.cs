// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace AntiforgerySample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Angular's default header name for sending the XSRF token.
            services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

            services.AddSingleton<TodoRepository>();
        }

        public void Configure(IApplicationBuilder app, IAntiforgery antiforgery, IOptions<AntiforgeryOptions> options, TodoRepository repository)
        {
            app.Use(next => context =>
            {
                if (
                    string.Equals(context.Request.Path.Value, "/", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(context.Request.Path.Value, "/index.html", StringComparison.OrdinalIgnoreCase))
                {
                    // We can send the request token as a JavaScript-readable cookie, and Angular will use it by default.
                    var tokens = antiforgery.GetAndStoreTokens(context);
                    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions() { HttpOnly = false });
                }

                return next(context);
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.Map("/api/items", a => a.Run(async context =>
            {
                if (string.Equals("GET", context.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    var items = repository.GetItems();
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(items));
                }
                else if (string.Equals("POST", context.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    // This will throw if the token is invalid.
                    await antiforgery.ValidateRequestAsync(context);

                    var serializer = new JsonSerializer();
                    using (var reader = new JsonTextReader(new StreamReader(context.Request.Body)))
                    {
                        var item = serializer.Deserialize<TodoItem>(reader);
                        repository.Add(item);
                    }

                    context.Response.StatusCode = 204;
                }
            }));
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseIISPlatformHandlerUrl()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
