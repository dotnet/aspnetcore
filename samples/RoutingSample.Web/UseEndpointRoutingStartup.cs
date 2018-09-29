// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoutingSample.Web
{
    public class UseEndpointRoutingStartup
    {
        private static readonly byte[] _homePayload = Encoding.UTF8.GetBytes("Endpoint Routing sample endpoints:" + Environment.NewLine + "/plaintext");
        private static readonly byte[] _plainTextPayload = Encoding.UTF8.GetBytes("Plain text!");

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<EndsWithStringRouteConstraint>();

            services.AddRouting(options =>
            {
                options.ConstraintMap.Add("endsWith", typeof(EndsWithStringRouteConstraint));
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseEndpointRouting(builder =>
            {
                builder.MapHello("/helloworld", "World");

                builder.MapEndpoint(
                    (httpContext) =>
                    {
                        var response = httpContext.Response;
                        var payloadLength = _homePayload.Length;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        response.ContentLength = payloadLength;
                        return response.Body.WriteAsync(_homePayload, 0, payloadLength);
                    },
                    "/",
                    "Home");
                builder.MapEndpoint(
                    (httpContext) =>
                    {
                        var response = httpContext.Response;
                        var payloadLength = _plainTextPayload.Length;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        response.ContentLength = payloadLength;
                        return response.Body.WriteAsync(_plainTextPayload, 0, payloadLength);
                    },
                    "/plaintext",
                    "Plaintext");
                builder.MapEndpoint(
                    (httpContext) =>
                    {
                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync("WithConstraints");
                    },
                    "/withconstraints/{id:endsWith(_001)}",
                    "withconstraints");
                builder.MapEndpoint(
                    (httpContext) =>
                    {
                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync("withoptionalconstraints");
                    },
                    "/withoptionalconstraints/{id:endsWith(_001)?}",
                    "withoptionalconstraints");
                builder.MapEndpoint(
                    (httpContext) =>
                    {
                        using (var writer = new StreamWriter(httpContext.Response.Body, Encoding.UTF8, 1024, leaveOpen: true))
                        {
                            var graphWriter = httpContext.RequestServices.GetRequiredService<DfaGraphWriter>();
                            var dataSource = httpContext.RequestServices.GetRequiredService<CompositeEndpointDataSource>();
                            graphWriter.Write(dataSource, writer);
                        }

                        return Task.CompletedTask;
                    },
                    "/graph",
                    "DFA Graph",
                    new HttpMethodMetadata(new[] { "GET", }));
                builder.MapEndpoint(
                    (httpContext) =>
                    {
                        var linkGenerator = httpContext.RequestServices.GetRequiredService<LinkGenerator>();

                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync(
                            "Link: " + linkGenerator.GetPathByRouteValues(httpContext, "WithSingleAsteriskCatchAll", new { }));
                    },
                    "/WithSingleAsteriskCatchAll/{*path}",
                    "WithSingleAsteriskCatchAll",
                    new RouteValuesAddressMetadata(routeName: "WithSingleAsteriskCatchAll", requiredValues: new RouteValueDictionary()));
                builder.MapEndpoint(
                    (httpContext) =>
                    {
                        var linkGenerator = httpContext.RequestServices.GetRequiredService<LinkGenerator>();

                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync(
                            "Link: " + linkGenerator.GetPathByRouteValues(httpContext, "WithDoubleAsteriskCatchAll", new { }));
                    },
                    "/WithDoubleAsteriskCatchAll/{**path}",
                    "WithDoubleAsteriskCatchAll",
                    new RouteValuesAddressMetadata(routeName: "WithDoubleAsteriskCatchAll", requiredValues: new RouteValueDictionary()));
            });

            app.UseStaticFiles();

            app.UseEndpoint();
        }
    }
}
