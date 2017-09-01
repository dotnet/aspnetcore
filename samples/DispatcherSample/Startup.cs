// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DispatcherSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<UrlGenerator>();
            services.AddSingleton<RouteValueAddressTable>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("<p>Middleware 1</p>");
                await next.Invoke();
            });

            var dictionary = new Dictionary<string, DispatcherFeature>
            {
                {
                    "/example",
                    new DispatcherFeature
                        {
                            Endpoint = new DispatcherEndpoint("example"),
                            RequestDelegate = async (context) =>
                            {
                                await context.Response.WriteAsync("Hello from the example!");
                            }
                        }
                },
                {
                    "/example2",
                    new DispatcherFeature
                        {
                            Endpoint = new DispatcherEndpoint("example2"),
                            RequestDelegate = async (context) =>
                            {
                                await context.Response.WriteAsync("Hello from the second example!");
                            }
                        }
                },
            };

            app.Use(async (context, next) =>
            {
                if (dictionary.TryGetValue(context.Request.Path, out var value))
                {
                    var dispatcherFeature = new DispatcherFeature
                    {
                        Endpoint = value.Endpoint,
                        RequestDelegate = value.RequestDelegate
                    };

                    context.Features.Set<IDispatcherFeature>(dispatcherFeature);
                    await context.Response.WriteAsync("<p>Dispatch</p>");
                    await next.Invoke();
                }
            });

            app.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("<p>Middleware 2</p>");
                await next.Invoke();
            });

            app.Use(async (context, next) =>
            {
                var feature = context.Features.Get<IDispatcherFeature>();
                await feature.RequestDelegate(context);
                await next.Invoke();
            });

            app.Run(async (context) =>
            {
                var urlGenerator = app.ApplicationServices.GetService<UrlGenerator>();
                var url = urlGenerator.GenerateURL(new RouteValueDictionary(new { Movie = "The Lion King", Character = "Mufasa" }), context);
                await context.Response.WriteAsync($"<p>Generated url: {url}</p>");
            });
        }
    }
}
