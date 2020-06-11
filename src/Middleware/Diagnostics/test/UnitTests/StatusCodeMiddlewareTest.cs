// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics
{
    public class StatusCodeMiddlewareTest
    {
        [Fact]
        public async Task Redirect_StatusPage()
        {
            var expectedStatusCode = 432;
            var destination = "/location";
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseStatusCodePagesWithRedirects("/errorPage?id={0}");

                    app.Map(destination, (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run((httpContext) =>
                        {
                            httpContext.Response.StatusCode = expectedStatusCode;
                            return Task.FromResult(1);
                        });
                    });

                    app.Map("/errorPage", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            await httpContext.Response.WriteAsync(httpContext.Request.QueryString.Value);
                        });
                    });

                    app.Run((context) =>
                    {
                        throw new InvalidOperationException($"Invalid input provided. {context.Request.Path}");
                    });
                });
            var expectedQueryString = $"?id={expectedStatusCode}";
            var expectedUri = $"/errorPage{expectedQueryString}";
            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var response = await client.GetAsync(destination);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(expectedUri, response.Headers.First(s => s.Key == "Location").Value.First());

            response = await client.GetAsync(expectedUri);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedQueryString, content);
            Assert.Equal(expectedQueryString, response.RequestMessage.RequestUri.Query);
        }

        [Fact]
        public async Task Reexecute_CanRetrieveInformationAboutOriginalRequest()
        {
            var expectedStatusCode = 432;
            var destination = "/location";
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        var beforeNext = context.Request.QueryString;
                        await next();
                        var afterNext = context.Request.QueryString;

                        Assert.Equal(beforeNext, afterNext);
                    });
                    app.UseStatusCodePagesWithReExecute(pathFormat: "/errorPage", queryFormat: "?id={0}");

                    app.Map(destination, (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run((httpContext) =>
                        {
                            httpContext.Response.StatusCode = expectedStatusCode;
                            return Task.FromResult(1);
                        });
                    });

                    app.Map("/errorPage", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            var statusCodeReExecuteFeature = httpContext.Features.Get<IStatusCodeReExecuteFeature>();
                            await httpContext.Response.WriteAsync(
                                httpContext.Request.QueryString.Value
                                + ", "
                                + statusCodeReExecuteFeature.OriginalPath
                                + ", "
                                + statusCodeReExecuteFeature.OriginalQueryString);
                        });
                    });

                    app.Run((context) =>
                    {
                        throw new InvalidOperationException("Invalid input provided.");
                    });
                });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var response = await client.GetAsync(destination + "?name=James");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal($"?id={expectedStatusCode}, /location, ?name=James", content);
        }

        [Fact]
        public async Task Reexecute_ClearsEndpointAndRouteData()
        {
            var expectedStatusCode = 432;
            var destination = "/location";
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseStatusCodePagesWithReExecute(pathFormat: "/errorPage", queryFormat: "?id={0}");

                    app.Use((context, next) =>
                    {
                        Assert.Empty(context.Request.RouteValues);
                        Assert.Null(context.GetEndpoint());
                        return next();
                    });

                    app.Map(destination, (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run((httpContext) =>
                        {
                            httpContext.SetEndpoint(new Endpoint((_) => Task.CompletedTask, new EndpointMetadataCollection(), "Test"));
                            httpContext.Request.RouteValues["John"] = "Doe";
                            httpContext.Response.StatusCode = expectedStatusCode;
                            return Task.CompletedTask;
                        });
                    });

                    app.Map("/errorPage", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            var statusCodeReExecuteFeature = httpContext.Features.Get<IStatusCodeReExecuteFeature>();
                            await httpContext.Response.WriteAsync(
                                httpContext.Request.QueryString.Value
                                + ", "
                                + statusCodeReExecuteFeature.OriginalPath
                                + ", "
                                + statusCodeReExecuteFeature.OriginalQueryString);
                        });
                    });

                    app.Run((context) =>
                    {
                        throw new InvalidOperationException("Invalid input provided.");
                    });
                });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var response = await client.GetAsync(destination + "?name=James");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal($"?id={expectedStatusCode}, /location, ?name=James", content);
        }
    }
}
