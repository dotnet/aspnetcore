// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;

namespace DispatcherSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<DispatcherOptions>(options =>
            {
                options.DispatcherEntryList = new List<DispatcherEntry>()
                {
                    new DispatcherEntry
                    {
                        RouteTemplate = TemplateParser.Parse("{Endpoint=example}"),
                        Endpoints = new []
                        {
                            new RouteValuesEndpoint("example")
                            {
                                RequiredValues = new RouteValueDictionary(new { Endpoint = "First" }),
                                RequestDelegate = async (context) =>
                                {
                                    await context.Response.WriteAsync("Hello from the example!");
                                }
                            },
                            new RouteValuesEndpoint("example2")
                            {
                                RequiredValues = new RouteValueDictionary(new { Endpoint = "Second" }),
                                RequestDelegate = async (context) =>
                                {
                                    await context.Response.WriteAsync("Hello from the second example!");
                                }
                            },
                        }
                    },

                    new DispatcherEntry
                    {
                        RouteTemplate = TemplateParser.Parse("{Endpoint=example}/{Parameter=foo}"),
                        Endpoints = new []
                        {
                            new RouteValuesEndpoint("example")
                            {
                                RequiredValues = new RouteValueDictionary(new { Endpoint = "First", Parameter = "param1"}),
                                RequestDelegate = async (context) =>
                                {
                                    await context.Response.WriteAsync("Hello from the example for foo!");
                                }
                            },
                            new RouteValuesEndpoint("example2")
                            {
                                RequiredValues = new RouteValueDictionary(new { Endpoint = "Second", Parameter = "param2"}),
                                RequestDelegate = async (context) =>
                                {
                                    await context.Response.WriteAsync("Hello from the second example for foo!");
                                }
                            },
                        }
                    }
                };
            });

            services.AddSingleton<UrlGenerator>();
            services.AddSingleton<RouteValueAddressTable>();
            services.AddDispatcher();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("<p>Middleware 1</p>");
                await next.Invoke();
            });

            app.UseDispatcher();

            app.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("<p>Middleware 2</p>");
                await next.Invoke();
            });

            app.Use(async (context, next) =>
            {
                var urlGenerator = app.ApplicationServices.GetService<UrlGenerator>();
                var url = urlGenerator.GenerateURL(new RouteValueDictionary(new { Movie = "The Lion King", Character = "Mufasa" }), context);
                await context.Response.WriteAsync($"<p>Generated url: {url}</p>");
                await next.Invoke();
            });
        }
    }
}
