// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests
{
    public class HeaderPropagationIntegrationTest
    {
        [Fact]
        public async Task HeaderInRequest_AddCorrectValue()
        {
            // Arrange
            var handler = new SimpleHandler();
            var builder = CreateBuilder(c =>
                c.Headers.Add(new HeaderPropagationEntry
                {
                    InputName = "in",
                    OutputName = "out",
                }),
                handler);
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage();
            request.Headers.Add("in", "test");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(handler.Headers.Contains("out"));
            Assert.Equal(new[] { "test" }, handler.Headers.GetValues("out"));
        }

        private IWebHostBuilder CreateBuilder(Action<HeaderPropagationOptions> configure, HttpMessageHandler primaryHandler)
        {
            return new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHeaderPropagation();
                    app.UseMiddleware<SimpleMiddleware>();
                })
                .ConfigureServices(services =>
                {
                    services.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
                        .ConfigureHttpMessageHandlerBuilder(b =>
                        {
                            b.PrimaryHandler = primaryHandler;
                        })
                        .AddHeaderPropagation();
                    services.AddHeaderPropagation(configure);
                });
        }

        private class SimpleHandler : DelegatingHandler
        {
            public HttpHeaders Headers { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Headers = request.Headers;
                return Task.FromResult(new HttpResponseMessage());
            }
        }

        private class SimpleMiddleware
        {
            private readonly IHttpClientFactory _httpClientFactory;

            public SimpleMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory)
            {
                _httpClientFactory = httpClientFactory;
            }

            public Task InvokeAsync(HttpContext _)
            {
                var client = _httpClientFactory.CreateClient("example.com");
                return client.GetAsync("");
            }
        }
    }
}
