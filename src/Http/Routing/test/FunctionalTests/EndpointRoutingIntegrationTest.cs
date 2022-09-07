// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Routing.FunctionalTests;

public class EndpointRoutingIntegrationTest
{
    private static readonly RequestDelegate TestDelegate = async context => await Task.Yield();
    private static readonly string AuthErrorMessage = "Endpoint / contains authorization metadata, but a middleware was not found that supports authorization." +
        Environment.NewLine +
        "Configure your application startup by adding app.UseAuthorization() in the application startup code. " +
        "If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseAuthorization() must go between them.";

    private static readonly string CORSErrorMessage = "Endpoint / contains CORS metadata, but a middleware was not found that supports CORS." +
        Environment.NewLine +
        "Configure your application startup by adding app.UseCors() in the application startup code. " +
        "If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseCors() must go between them.";

    [Fact]
    public async Task AuthorizationMiddleware_WhenNoAuthMetadataIsConfigured()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthorization();
                        app.UseEndpoints(b => b.Map("/", TestDelegate));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddAuthorization();
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var response = await server.CreateRequest("/").SendAsync("GET");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AuthorizationMiddleware_WhenEndpointIsNotFound()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthorization();
                        app.UseEndpoints(b => b.Map("/", TestDelegate));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddAuthorization();
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var response = await server.CreateRequest("/not-found").SendAsync("GET");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationMiddleware_WithAuthorizedEndpoint()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthorization();
                        app.UseEndpoints(b => b.Map("/", TestDelegate).RequireAuthorization());
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var response = await server.CreateRequest("/").SendAsync("GET");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AuthorizationMiddleware_NotConfigured_Throws()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(b => b.Map("/", TestDelegate).RequireAuthorization());

                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => server.CreateRequest("/").SendAsync("GET"));
        Assert.Equal(AuthErrorMessage, ex.Message);
    }

    [Fact]
    public async Task AuthorizationMiddleware_NotConfigured_WhenEndpointIsNotFound()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(b => b.Map("/", TestDelegate).RequireAuthorization());
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var response = await server.CreateRequest("/not-found").SendAsync("GET");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationMiddleware_ConfiguredBeforeRouting_Throws()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseAuthorization();
                        app.UseRouting();
                        app.UseEndpoints(b => b.Map("/", TestDelegate).RequireAuthorization());
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => server.CreateRequest("/").SendAsync("GET"));
        Assert.Equal(AuthErrorMessage, ex.Message);
    }

    [Fact]
    public async Task AuthorizationMiddleware_ConfiguredAfterRouting_Throws()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(b => b.Map("/", TestDelegate).RequireAuthorization());
                        app.UseAuthorization();
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => server.CreateRequest("/").SendAsync("GET"));
        Assert.Equal(AuthErrorMessage, ex.Message);
    }

    [Fact]
    public async Task CorsMiddleware_WithCorsEndpoint()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseCors();
                        app.UseEndpoints(b => b.Map("/", TestDelegate).RequireCors(policy => policy.AllowAnyOrigin()));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddCors();
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var response = await server.CreateRequest("/").SendAsync("PUT");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CorsMiddleware_WithCorsEndpoint_PreflightRequest()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseCors();
                        app.UseEndpoints(b => b.MapPut("/", TestDelegate).RequireCors(policy => policy.AllowAnyOrigin().AllowAnyMethod()));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddCors();
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var request = server.CreateRequest("/");
        request.AddHeader(HeaderNames.Origin, "http://testlocation.com");
        request.AddHeader(HeaderNames.AccessControlRequestMethod, HttpMethods.Put);

        var response = await request.SendAsync(HttpMethods.Options);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CorsMiddleware_ConfiguredBeforeRouting_Throws()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseCors();
                        app.UseRouting();
                        app.UseEndpoints(b => b.Map("/", TestDelegate).RequireCors(policy => policy.AllowAnyOrigin()));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddCors();
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => server.CreateRequest("/").SendAsync("GET"));
        Assert.Equal(CORSErrorMessage, ex.Message);
    }
}
