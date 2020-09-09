// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh.Tests
{
    public class ResponseStreamWrapperTest
    {
        private const string BrowserAcceptHeader = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";

        [Fact]
        public async Task HtmlIsInjectedForStaticFiles()
        {
            // Arrange
            using var host = await StartHostAsync();
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/index.html").AddHeader("Accept", BrowserAcceptHeader).SendAsync("GET");

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsInjectedForLargeStaticFiles()
        {
            // Arrange
            using var host = await StartHostAsync();
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/largefile.html").AddHeader("Accept", BrowserAcceptHeader).SendAsync("GET");

            // Assert
            response.EnsureSuccessStatusCode();

            Assert.False(response.Content.Headers.TryGetValues("Content-Length", out _), "We shouldn't send a Content-Length header.");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsInjectedForDynamicallyGeneratedMarkup()
        {
            // Arrange
            using var host = await StartHostAsync(routes =>
            {
                routes.MapGet("/dynamic-html", async context =>
                {
                    context.Response.Headers["Content-Type"] = "text/html;charset=utf-8";
                    await context.Response.WriteAsync("<html><body><h1>Hello world</h1></body></html>");
                });
            });
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/dynamic-html").AddHeader("Accept", BrowserAcceptHeader).SendAsync("GET");

            // Assert
            response.EnsureSuccessStatusCode();

            Assert.False(response.Content.Headers.TryGetValues("Content-Length", out _), "We shouldn't send a Content-Length header.");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsInjectedForWriteAsyncMarkupWithContentLength()
        {
            // Arrange
            var responseContent = Encoding.UTF8.GetBytes("<html><body><h1>Hello world</h1></body></html>");
            using var host = await StartHostAsync(routes =>
            {
                routes.MapGet("/dynamic-html", async context =>
                {
                    context.Response.ContentLength = responseContent.Length;
                    context.Response.Headers["Content-Type"] = "text/html;charset=utf-8";
                    await context.Response.Body.WriteAsync(responseContent);
                });
            });
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/dynamic-html").AddHeader("Accept", BrowserAcceptHeader).SendAsync("GET");

            // Assert
            response.EnsureSuccessStatusCode();

            Assert.False(response.Content.Headers.TryGetValues("Content-Length", out _), "We shouldn't send a Content-Length header.");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsInjectedForWriteAsyncMarkupWithMultipleWrites()
        {
            // Arrange
            using var host = await StartHostAsync(routes =>
            {
                routes.MapGet("/dynamic-html", async context =>
                {
                    context.Response.Headers["Content-Type"] = "text/html;charset=utf-8";

                    await context.Response.WriteAsync("<html>");
                    await context.Response.WriteAsync("<body>");
                    await context.Response.WriteAsync("<ul>");
                    for (var i = 0; i < 100; i++)
                    {
                        await context.Response.WriteAsync($"<li>{i}</li>");
                    }
                    await context.Response.WriteAsync("</ul>");
                    await context.Response.WriteAsync("</body>");
                    await context.Response.WriteAsync("</html>");
                });
            });
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/dynamic-html").AddHeader("Accept", BrowserAcceptHeader).SendAsync("GET");

            // Assert
            response.EnsureSuccessStatusCode();

            Assert.False(response.Content.Headers.TryGetValues("Content-Length", out _), "We shouldn't send a Content-Length header.");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsInjectedForPostResponses()
        {
            // Arrange
            using var host = await StartHostAsync(routes =>
            {
                routes.MapPost("/mvc-view", async context =>
                {
                    context.Response.Headers["Content-Type"] = "text/html;charset=utf-8";

                    await context.Response.WriteAsync("<html>");
                    await context.Response.WriteAsync("<body>");
                    await context.Response.WriteAsync("<ul>");
                    for (var i = 0; i < 100; i++)
                    {
                        await context.Response.WriteAsync($"<li>{i}</li>");
                    }
                    await context.Response.WriteAsync("</ul>");
                    await context.Response.WriteAsync("</body>");
                    await context.Response.WriteAsync("</html>");
                });
            });
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/mvc-view").AddHeader("Accept", BrowserAcceptHeader).SendAsync("POST");

            // Assert
            response.EnsureSuccessStatusCode();

            Assert.False(response.Content.Headers.TryGetValues("Content-Length", out _), "We shouldn't send a Content-Length header.");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsNotInjectedForNonBrowserRequests()
        {
            // Arrange
            using var host = await StartHostAsync();
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/favicon.ico").AddHeader("Accept", "application/json").SendAsync("GET");

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsNotInjectedForNonHtmlResponses()
        {
            // Arrange
            using var host = await StartHostAsync();
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/favicon.ico").AddHeader("Accept", BrowserAcceptHeader).SendAsync("GET");

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsNotInjectedForNon200Responses()
        {
            // Arrange
            using var host = await StartHostAsync();
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/file-does-not-exist.html").AddHeader("Accept", BrowserAcceptHeader).SendAsync("GET");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsNotInjectedForNonGetOrPostResponses()
        {
            // Arrange
            var responseContent = "<html><body><h1>Hello world</h1></body></html>";
            using var host = await StartHostAsync(routes =>
            {
                routes.MapMethods(
                    "/dynamic-html",
                    new[] { "HEAD", "GET", "DELETE" },
                    async context =>
                    {
                        context.Response.Headers["Content-Type"] = "text/html;charset=utf-8";
                        await context.Response.WriteAsync(responseContent);
                    });
            });
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/dynamic-html").AddHeader("Accept", BrowserAcceptHeader).SendAsync("HEAD");

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsNotInjectedForJsonResponses()
        {
            // Arrange
            var responseContent = "<html><body><h1>Hello world</h1></body></html>";
            using var host = await StartHostAsync(routes =>
            {
                routes.MapGet(
                    "/dynamic-html",
                    async context =>
                    {
                        context.Response.Headers["Content-Type"] = "application/json;charset=utf-8";
                        await context.Response.WriteAsync(responseContent);
                    });
            });
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/dynamic-html").AddHeader("Accept", BrowserAcceptHeader).SendAsync("GET");

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("dotnet-watch browser reload script", content);
        }

        [Fact]
        public async Task HtmlIsNotInjectedForNonUtf8Responses()
        {
            // Arrange
            var responseContent = "<html><body><h1>Hello world</h1></body></html>";
            using var host = await StartHostAsync(routes =>
            {
                routes.MapGet(
                    "/dynamic-html",
                    async context =>
                    {
                        context.Response.Headers["Content-Type"] = "text/html;charset=utf-16";
                        await context.Response.Body.WriteAsync(Encoding.Unicode.GetBytes(responseContent));
                    });
            });
            using var server = host.GetTestServer();
            var response = await server.CreateRequest("/dynamic-html").AddHeader("Accept", BrowserAcceptHeader).SendAsync("GET");

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("dotnet-watch browser reload script", content);
        }

        private static async Task<IHost> StartHostAsync(Action<IEndpointRouteBuilder>? routeBuilder = null)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseMiddleware<BrowserRefreshMiddleware>();
                        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "wwwroot")) });

                        if (routeBuilder != null)
                        {
                            app.UseRouting();
                            app.UseEndpoints(routeBuilder);
                        }
                    })
                    .ConfigureServices(services => services.AddRouting());
                }).Build();

            await host.StartAsync();
            return host;
        }
    }
}
