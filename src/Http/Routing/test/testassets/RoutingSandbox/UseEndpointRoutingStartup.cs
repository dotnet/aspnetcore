// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;
using RoutingSandbox.Framework;

namespace RoutingSandbox
{
    public class UseEndpointRoutingStartup
    {
        private static readonly byte[] _homePayload = Encoding.UTF8.GetBytes("Endpoint Routing sample endpoints:" + Environment.NewLine + "/plaintext");
        private static readonly byte[] _plainTextPayload = Encoding.UTF8.GetBytes("Plain text!");

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting(options =>
            {
                options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
            });
            services.AddSingleton<FrameworkEndpointDataSource>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHello("/helloworld", "World");

                endpoints.MapGet(
                    "/",
                    (httpContext) =>
                    {
                        var dataSource = httpContext.RequestServices.GetRequiredService<EndpointDataSource>();

                        var sb = new StringBuilder();
                        sb.AppendLine("Endpoints:");
                        foreach (var endpoint in dataSource.Endpoints.OfType<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText, StringComparer.OrdinalIgnoreCase))
                        {
                            sb.AppendLine($"- {endpoint.RoutePattern.RawText}");
                            foreach (var metadata in endpoint.Metadata)
                            {
                                sb.AppendLine("    " + metadata);
                            }
                        }

                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync(sb.ToString());
                    });
                endpoints.MapGet(
                    "/plaintext",
                    (httpContext) =>
                    {
                        var response = httpContext.Response;
                        var payloadLength = _plainTextPayload.Length;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        response.ContentLength = payloadLength;
                        return response.Body.WriteAsync(_plainTextPayload, 0, payloadLength);
                    });
                endpoints.MapGet(
                    "/graph",
                    (httpContext) =>
                    {
                        using (var writer = new StreamWriter(httpContext.Response.Body, Encoding.UTF8, 1024, leaveOpen: true))
                        {
                            var graphWriter = httpContext.RequestServices.GetRequiredService<DfaGraphWriter>();
                            var dataSource = httpContext.RequestServices.GetRequiredService<EndpointDataSource>();
                            graphWriter.Write(dataSource, writer);
                        }

                        return Task.CompletedTask;
                    }).WithDisplayName("DFA Graph");

                endpoints.MapGet("/attributes", HandlerWithAttributes);

                endpoints.Map("/getwithattributes", Handler);

                endpoints.MapFramework(frameworkBuilder =>
                {
                    frameworkBuilder.AddPattern("/transform/{hub:slugify=TestHub}/{method:slugify=TestMethod}");
                    frameworkBuilder.AddPattern("/{hub}/{method=TestMethod}");

                    frameworkBuilder.AddHubMethod("TestHub", "TestMethod", context => context.Response.WriteAsync("TestMethod!"));
                    frameworkBuilder.AddHubMethod("Login", "Authenticate", context => context.Response.WriteAsync("Authenticate!"));
                    frameworkBuilder.AddHubMethod("Login", "Logout", context => context.Response.WriteAsync("Logout!"));
                });
            });

        }

        [Authorize]
        private Task HandlerWithAttributes(HttpContext context)
        {
            return context.Response.WriteAsync("I have ann authorize attribute");
        }

        [HttpGet]
        private Task Handler(HttpContext context)
        {
            return context.Response.WriteAsync("I have a method metadata attribute");
        }

        private class AuthorizeAttribute : Attribute
        {

        }

        private class HttpGetAttribute : Attribute, IHttpMethodMetadata
        {
            public bool AcceptCorsPreflight => false;

            public IReadOnlyList<string> HttpMethods { get; } = new List<string> { "GET" };
        }
    }
}
