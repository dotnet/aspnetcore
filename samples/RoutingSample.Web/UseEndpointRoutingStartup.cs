// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoutingSample.Web
{
    public class UseEndpointRoutingStartup
    {
        private static readonly byte[] _homePayload = Encoding.UTF8.GetBytes("Endpoint Routing sample endpoints:" + Environment.NewLine + "/plaintext");
        private static readonly byte[] _helloWorldPayload = Encoding.UTF8.GetBytes("Hello, World!");

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<EndsWithStringMatchProcessor>();

            services.AddRouting(options =>
            {
                options.ConstraintMap.Add("endsWith", typeof(EndsWithStringMatchProcessor));
            });

            var endpointDataSource = new DefaultEndpointDataSource(new[]
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
                        0,
                        EndpointMetadataCollection.Empty,
                        "withoptionalconstraints"),
                    new MatcherEndpoint((next) => (httpContext) =>
                    {
                        using (var writer = new StreamWriter(httpContext.Response.Body, Encoding.UTF8, 1024, leaveOpen: true))
                        {
                            var graphWriter = httpContext.RequestServices.GetRequiredService<DfaGraphWriter>();
                            var dataSource = httpContext.RequestServices.GetRequiredService<CompositeEndpointDataSource>();
                            graphWriter.Write(dataSource, writer);
                        }

                        return Task.CompletedTask;
                    },
                    RoutePatternFactory.Parse("/graph"),
                    0,
                    new EndpointMetadataCollection(new HttpMethodMetadata(new[]{ "GET", })),
                    "DFA Graph"),
                });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<EndpointDataSource>(endpointDataSource));
        }

        public void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder app)
        {
            app.UseEndpointRouting();

            // Imagine some more stuff here...

            app.UseEndpoint();
        }
    }
}
