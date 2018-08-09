// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingSample.Web
{
    public class UseEndpointRoutingStartup
    {
        private static readonly byte[] _homePayload = Encoding.UTF8.GetBytes("Endpoint Routing sample endpoints:" + Environment.NewLine + "/plaintext");
        private static readonly byte[] _plainTextPayload = Encoding.UTF8.GetBytes("Plain text!");

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<EndsWithStringMatchProcessor>();

            services.AddRouting(options =>
            {
                options.ConstraintMap.Add("endsWith", typeof(EndsWithStringMatchProcessor));
            });
        }

        public void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder app)
        {
            app.UseEndpointRouting(builder =>
            {
                builder.MapHello("/helloworld", "World");

                builder.MapEndpoint(
                    (next) => (httpContext) =>
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
                    (next) => (httpContext) =>
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
                    (next) => (httpContext) =>
                    {
                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync("WithConstraints");
                    },
                    "/withconstraints/{id:endsWith(_001)}",
                    "withconstraints");
                builder.MapEndpoint(
                    (next) => (httpContext) =>
                    {
                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync("withoptionalconstraints");
                    },
                    "/withoptionalconstraints/{id:endsWith(_001)?}",
                    "withoptionalconstraints");
            });

            // Imagine some more stuff here...

            app.UseEndpoint();
        }
    }
}
