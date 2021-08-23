// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
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
            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
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
                }).Build();

            await host.StartAsync();

            var expectedQueryString = $"?id={expectedStatusCode}";
            var expectedUri = $"/errorPage{expectedQueryString}";
            using var server = host.GetTestServer();
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
            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.Use(async (context, next) =>
                        {
                            var beforeNext = context.Request.QueryString;
                            await next(context);
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
                }).Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
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
            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseStatusCodePagesWithReExecute(pathFormat: "/errorPage", queryFormat: "?id={0}");

                        app.Use((context, next) =>
                        {
                            Assert.Empty(context.Request.RouteValues);
                            Assert.Null(context.GetEndpoint());
                            return next(context);
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
                }).Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var response = await client.GetAsync(destination + "?name=James");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal($"?id={expectedStatusCode}, /location, ?name=James", content);
        }

        [Fact]
        public async Task Reexecute_WorksAfterUseRoutingWithGlobalRouteBuilder()
        {
            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                    })
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.Properties["__GlobalEndpointRouteBuilder"] = app.Properties["__EndpointRouteBuilder"];

                        // Mock app builder to simulate WebApplicationBuilders New() method
                        var mockApp = new Mock<IApplicationBuilder>();
                        mockApp.Setup(m => m.New()).Returns(() =>
                        {
                            app.Properties.Remove("__GlobalEndpointRouteBuilder");
                            return app.New();
                        });
                        mockApp.Setup(m => m.ApplicationServices).Returns(app.ApplicationServices);
                        mockApp.Setup(m => m.Properties).Returns(app.Properties);
                        mockApp.Setup(m => m.Build()).Returns(() => app.Build());
                        mockApp.Setup(m => m.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
                            .Returns<Func<RequestDelegate, RequestDelegate>>((f) => app.Use(f));

                        mockApp.Object.UseStatusCodePagesWithReExecute(pathFormat: "/errorPage", queryFormat: "?id={0}");

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/", c =>
                            {
                                c.Response.StatusCode = 404;
                                return Task.CompletedTask;
                            });

                            endpoints.MapGet("/errorPage", () => "errorPage");
                        });

                        app.Run((context) =>
                        {
                            throw new InvalidOperationException("Invalid input provided.");
                        });
                    });
                }).Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var response = await client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("errorPage", content);
        }
    }
}
