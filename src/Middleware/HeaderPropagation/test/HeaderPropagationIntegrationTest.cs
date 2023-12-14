// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests;

public class HeaderPropagationIntegrationTest
{
    [Fact]
    public async Task HeaderPropagation_WithoutMiddleware_Throws()
    {
        // Arrange
        Exception captured = null;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddHttpClient("test").AddHeaderPropagation();
                    services.AddHeaderPropagation(options =>
                    {
                        options.Headers.Add("X-TraceId");
                    });
                })
                .Configure(app =>
                {
                    // note: no header propagation middleware

                    app.Run(async context =>
                    {
                        try
                        {
                            var client = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("test");
                            await client.GetAsync("http://localhost/"); // will throw
                        }
                        catch (Exception ex)
                        {
                            captured = ex;
                        }
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var request = new HttpRequestMessage();

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.IsType<InvalidOperationException>(captured);
        Assert.Equal(
            "The HeaderPropagationValues.Headers property has not been initialized. Register the header propagation middleware " +
            "by adding 'app.UseHeaderPropagation()' in the 'Configure(...)' method. Header propagation can only be used within " +
            "the context of an HTTP request.",
            captured.Message);
    }

    [Fact]
    public async Task HeaderPropagation_OutsideOfIncomingRequest_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("test").AddHeaderPropagation();
        services.AddHeaderPropagation(options =>
        {
            options.Headers.Add("X-TraceId");
        });
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("test");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("http://localhost/"));
        Assert.Equal(
            "The HeaderPropagationValues.Headers property has not been initialized. Register the header propagation middleware " +
            "by adding 'app.UseHeaderPropagation()' in the 'Configure(...)' method. Header propagation can only be used within " +
            "the context of an HTTP request.",
            exception.Message);
    }

    [Fact]
    public async Task HeaderInRequest_AddCorrectValue()
    {
        // Arrange
        var handler = new SimpleHandler();
        using var host = await CreateHost(c =>
            c.Headers.Add("in", "out"),
            handler);
        var server = host.GetTestServer();
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

    [Fact]
    public async Task MultipleHeaders_HeadersInRequest_AddAllHeaders()
    {
        // Arrange
        var handler = new SimpleHandler();
        using var host = await CreateHost(c =>
            {
                c.Headers.Add("first");
                c.Headers.Add("second");
            },
            handler);
        var server = host.GetTestServer();
        var client = server.CreateClient();

        var request = new HttpRequestMessage();
        request.Headers.Add("first", "value");
        request.Headers.Add("second", "other");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(handler.Headers.Contains("first"));
        Assert.Equal(new[] { "value" }, handler.Headers.GetValues("first"));
        Assert.True(handler.Headers.Contains("second"));
        Assert.Equal(new[] { "other" }, handler.Headers.GetValues("second"));
    }

    [Fact]
    public async Task Builder_UseHeaderPropagation_Without_AddHeaderPropagation_Throws()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHeaderPropagation();
                });
            }).Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
        Assert.Equal(
            "Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddHeaderPropagation' inside the call to 'ConfigureServices(...)' in the application startup code.",
            exception.Message);
    }

    [Fact]
    public async Task HeaderInRequest_OverrideHeaderPerClient_AddCorrectValue()
    {
        // Arrange
        var handler = new SimpleHandler();
        using var host = await CreateHost(
            c => c.Headers.Add("in", "out"),
            handler,
            c => c.Headers.Add("out", "different"));
        var server = host.GetTestServer();
        var client = server.CreateClient();

        var request = new HttpRequestMessage();
        request.Headers.Add("in", "test");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(handler.Headers.Contains("different"));
        Assert.Equal(new[] { "test" }, handler.Headers.GetValues("different"));
    }

    private async Task<IHost> CreateHost(Action<HeaderPropagationOptions> configure, HttpMessageHandler primaryHandler, Action<HeaderPropagationMessageHandlerOptions> configureClient = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHeaderPropagation();
                    app.UseMiddleware<SimpleMiddleware>();
                })
                .ConfigureServices(services =>
                {
                    services.AddHeaderPropagation(configure);
                    var client = services.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
                        .ConfigurePrimaryHttpMessageHandler(() => primaryHandler);

                    if (configureClient != null)
                    {
                        client.AddHeaderPropagation(configureClient);
                    }
                    else
                    {
                        client.AddHeaderPropagation();
                    }
                });
            }).Build();

        await host.StartAsync();

        return host;
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
