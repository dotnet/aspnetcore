// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace DeveloperExceptionPageSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Use((context, next) =>
            {
                context.Request.RouteValues = new RouteValueDictionary(new
                {
                    routeValue1 = "Value1",
                    routeValue2 = "Value2",
                });

                var endpoint = new RouteEndpoint(
                    c => null,
                    RoutePatternFactory.Parse("/"),
                    0,
                    new EndpointMetadataCollection(new HttpMethodMetadata(new[] { "GET", "POST" })),
                    "Endpoint display name");

                context.SetEndpoint(endpoint);
                return next();
            });
            app.UseDeveloperExceptionPage();
            app.Run(context =>
            {
                throw new Exception(string.Concat(
                    "Demonstration exception. The list:", "\r\n",
                    "New Line 1", "\n",
                    "New Line 2", Environment.NewLine,
                    "New Line 3"));
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
