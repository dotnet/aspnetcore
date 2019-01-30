// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Endpoints;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoutingWebSite
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
            app.UseRouting(routes =>
            {
                routes.MapHello("/helloworld", "World");

                routes.MapGet(
                    "/",
                    (httpContext) =>
                    {
                        var dataSource = httpContext.RequestServices.GetRequiredService<EndpointDataSource>();

                        var sb = new StringBuilder();
                        sb.AppendLine("Endpoints:");
                        foreach (var endpoint in dataSource.Endpoints.OfType<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText, StringComparer.OrdinalIgnoreCase))
                        {
                            sb.AppendLine($"- {endpoint.RoutePattern.RawText}");
                        }

                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync(sb.ToString());
                    });
                routes.MapGet(
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
                routes.MapGet(
                    "/convention",
                    (httpContext) =>
                    {
                        var endpoint = httpContext.GetEndpoint();
                        return httpContext.Response.WriteAsync((endpoint.Metadata.GetMetadata<CustomMetadata>() != null) ? "Has metadata" : "No metadata");
                    }).Add(b =>
                    {
                        b.Metadata.Add(new CustomMetadata());
                    });
                routes.MapGet(
                    "/withconstraints/{id:endsWith(_001)}",
                    (httpContext) =>
                    {
                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync("WithConstraints");
                    });
                routes.MapGet(
                    "/withoptionalconstraints/{id:endsWith(_001)?}",
                    (httpContext) =>
                    {
                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync("withoptionalconstraints");
                    });
                routes.MapGet(
                    "/WithSingleAsteriskCatchAll/{*path}",
                    (httpContext) =>
                    {
                        var linkGenerator = httpContext.RequestServices.GetRequiredService<LinkGenerator>();

                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync(
                            "Link: " + linkGenerator.GetPathByRouteValues(httpContext, "WithSingleAsteriskCatchAll", new { }));
                    },
                    new RouteNameMetadata(routeName: "WithSingleAsteriskCatchAll"));
                routes.MapGet(
                    "/WithDoubleAsteriskCatchAll/{**path}",
                    (httpContext) =>
                    {
                        var linkGenerator = httpContext.RequestServices.GetRequiredService<LinkGenerator>();

                        var response = httpContext.Response;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        return response.WriteAsync(
                            "Link: " + linkGenerator.GetPathByRouteValues(httpContext, "WithDoubleAsteriskCatchAll", new { }));
                    },
                    new RouteNameMetadata(routeName: "WithDoubleAsteriskCatchAll"));

                MapHostEndpoint(routes);
                MapHostEndpoint(routes, "*.0.0.1");
                MapHostEndpoint(routes, "127.0.0.1");
                MapHostEndpoint(routes, "*.0.0.1:5000", "*.0.0.1:5001");
                MapHostEndpoint(routes, "contoso.com:*", "*.contoso.com:*");
            });

            app.Map("/Branch1", branch => SetupBranch(branch, "Branch1"));
            app.Map("/Branch2", branch => SetupBranch(branch, "Branch2"));

            app.UseStaticFiles();

            // Imagine some more stuff here...

            app.UseEndpoint();
        }

        private class CustomMetadata
        {
        }

        private IEndpointConventionBuilder MapHostEndpoint(IEndpointRouteBuilder routes, params string[] hosts)
        {
            var hostsDisplay = (hosts == null || hosts.Length == 0)
                ? "*:*"
                : string.Join(",", hosts.Select(h => h.Contains(':') ? h : h + ":*"));

            var conventionBuilder = routes.MapGet(
                "api/DomainWildcard",
                httpContext =>
                {
                    var response = httpContext.Response;
                    response.StatusCode = 200;
                    response.ContentType = "text/plain";
                    return response.WriteAsync(hostsDisplay);
                });

            conventionBuilder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(new HostAttribute(hosts));
                endpointBuilder.DisplayName += " HOST: " + hostsDisplay;
            });

            return conventionBuilder;
        }

        private void SetupBranch(IApplicationBuilder app, string name)
        {
            app.UseRouting(routes =>
            {
                routes.MapGet("api/get/{id}", (context) => context.Response.WriteAsync($"{name} - API Get {context.GetRouteData().Values["id"]}"));
            });

            app.UseEndpoint();
        }
    }
}
