// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace DispatcherSample.Web
{
    public class Startup
    {
        private static readonly byte[] _homePayload = Encoding.UTF8.GetBytes("Dispatcher sample endpoints:" + Environment.NewLine + "/plaintext");
        private static readonly byte[] _helloWorldPayload = Encoding.UTF8.GetBytes("Hello, World!");

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<EndsWithStringMatchProcessor>();

            services.AddRouting(options =>
            {
                options.ConstraintMap.Add("endsWith", typeof(EndsWithStringMatchProcessor));
            });

            services.AddDispatcher(options =>
            {
                options.DataSources.Add(new DefaultEndpointDataSource(new[]
                {
                    new MatcherEndpoint((next) => (httpContext) =>
                        {
                            var response = httpContext.Response;
                            var payloadLength = _homePayload.Length;
                            response.StatusCode = 200;
                            response.ContentType = "text/plain";
                            response.ContentLength = payloadLength;
                            return response.Body.WriteAsync(_homePayload, 0, payloadLength);
                        },
                        RoutePatternFactory.Parse("/"),
                        new RouteValueDictionary(),
                        0,
                        EndpointMetadataCollection.Empty,
                        "Home"),
                    new MatcherEndpoint((next) => (httpContext) =>
                        {
                            var response = httpContext.Response;
                            var payloadLength = _helloWorldPayload.Length;
                            response.StatusCode = 200;
                            response.ContentType = "text/plain";
                            response.ContentLength = payloadLength;
                            return response.Body.WriteAsync(_helloWorldPayload, 0, payloadLength);
                        },
                         RoutePatternFactory.Parse("/plaintext"),
                         new RouteValueDictionary(),
                        0,
                        EndpointMetadataCollection.Empty,
                        "Plaintext"),
                    new MatcherEndpoint((next) => (httpContext) =>
                        {
                            var response = httpContext.Response;
                            response.StatusCode = 200;
                            response.ContentType = "text/plain";
                            return response.WriteAsync("WithConstraints");
                        },
                        RoutePatternFactory.Parse("/withconstraints/{id:endsWith(_001)}"),
                        new RouteValueDictionary(),
                        0,
                        EndpointMetadataCollection.Empty,
                        "withconstraints"),
                    new MatcherEndpoint((next) => (httpContext) =>
                        {
                            var response = httpContext.Response;
                            response.StatusCode = 200;
                            response.ContentType = "text/plain";
                            return response.WriteAsync("withoptionalconstraints");
                        },
                        RoutePatternFactory.Parse("/withoptionalconstraints/{id:endsWith(_001)?}"),
                        new RouteValueDictionary(),
                        0,
                        EndpointMetadataCollection.Empty,
                        "withoptionalconstraints"),
                }));
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDispatcher();

            // Imagine some more stuff here...

            app.UseEndpoint();
        }
    }
}
