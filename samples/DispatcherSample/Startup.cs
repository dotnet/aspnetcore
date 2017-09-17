// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Dispatcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DispatcherSample
{
    public class Startup
    {
        private readonly static IInlineConstraintResolver ConstraintResolver = new DefaultInlineConstraintResolver(
            new OptionsManager<RouteOptions>(
                new OptionsFactory<RouteOptions>(
                    Enumerable.Empty<IConfigureOptions<RouteOptions>>(),
                    Enumerable.Empty<IPostConfigureOptions<RouteOptions>>())));

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<DispatcherOptions>(options =>
            {
                options.Dispatchers.Add(CreateDispatcher(
                    "{Endpoint=example}",
                    new RouteValuesEndpoint(
                        new RouteValueDictionary(new { Endpoint = "First" }),
                        async (context) =>
                        {
                            await context.Response.WriteAsync("Hello from the example!");
                        },
                        Array.Empty<object>(),
                        "example"),
                    new RouteValuesEndpoint(
                        new RouteValueDictionary(new { Endpoint = "Second" }),
                        async (context) =>
                        {
                            await context.Response.WriteAsync("Hello from the second example!");
                        },
                        Array.Empty<object>(),
                        "example2")));

                options.Dispatchers.Add(CreateDispatcher(
                    "{Endpoint=example}/{Parameter=foo}",
                    new RouteValuesEndpoint(
                        new RouteValueDictionary(new { Endpoint = "First", Parameter = "param1" }),
                        async (context) =>
                        {
                            await context.Response.WriteAsync("Hello from the example for foo!");
                        },
                        Array.Empty<object>(),
                        "example"),
                    new RouteValuesEndpoint(
                        new RouteValueDictionary(new { Endpoint = "Second", Parameter = "param2" }),
                        async (context) =>
                        {
                            await context.Response.WriteAsync("Hello from the second example for foo!");
                        },
                        Array.Empty<object>(),
                        "example2")));

                options.HandlerFactories.Add((endpoint) => (endpoint as RouteValuesEndpoint)?.HandlerFactory);
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

        private static RequestDelegate CreateDispatcher(string routeTemplate, RouteValuesEndpoint endpoint, params RouteValuesEndpoint[] endpoints)
        {
            var dispatcher = new RouterDispatcher(new Route(new RouterEndpointSelector(new[] { endpoint }.Concat(endpoints)), routeTemplate, ConstraintResolver));
            return dispatcher.InvokeAsync;
        }
    }
}
