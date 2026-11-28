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

    [Theory]
    [InlineData("/store/inventory/atlas-deluxe/details", "Featured:")]
    [InlineData("/store/inventory/atlas-deluxe/stock", "Stock:edition=deluxe,sku=atlas,tenant=store")]
    [InlineData("/outlet/inventory/river-standard/details", "Details:edition=standard,sku=river,tenant=outlet")]
    [InlineData("/store/inventory/clearance/details", "Item:item=clearance,tenant=store")]
    public async Task Routing_LiteralAndComplexSuffixesPreserveFallbackAndCaptures(string path, string expectedBody)
    {
        await AssertRouteResponseAsync(
            path,
            expectedBody,
            ("/{tenant}/inventory/{item}/details", "Item"),
            ("/{tenant}/inventory/{sku}-{edition}/details", "Details"),
            ("/{tenant}/inventory/{sku}-{edition}/stock", "Stock"),
            ("/store/inventory/atlas-deluxe/details", "Featured"));
    }

    [Theory]
    [InlineData("/orders/42/lines", "NumberedLines:id=42")]
    [InlineData("/orders/current/lines", "NamedLines:slug=current")]
    [InlineData("/orders/77/lines", "NumberedLines:id=77")]
    public async Task Routing_ConstrainedParameterPreservesLiteralAndGenericMatches(string path, string expectedBody)
    {
        await AssertRouteResponseAsync(
            path,
            expectedBody,
            ("/orders/{slug}/lines", "NamedLines"),
            ("/orders/{id:int}/lines", "NumberedLines"),
            ("/orders/42/receipt", "NumberedReceipt"),
            ("/orders/current/receipt", "CurrentReceipt"));
    }

    [Theory]
    [InlineData("/library/local/archive", "Asset:asset=<null>,shelf=local")]
    [InlineData("/library/remote/archive/readme.txt", "Asset:asset=readme.txt,shelf=remote")]
    [InlineData("/library/local/archive/assets/icons/logo.svg", "Asset:asset=assets/icons/logo.svg,shelf=local")]
    [InlineData("/library/local/archive/index", "Index:")]
    [InlineData("/library/local/archive/pages/release.json/summary", "Page:page=release.json/summary,shelf=local")]
    [InlineData("/library/remote/archive/pages/2026/release.json", "Asset:asset=pages/2026/release.json,shelf=remote")]
    public async Task Routing_CatchAllsPreserveLiteralPrecedenceAndConstraintFallback(string path, string expectedBody)
    {
        await AssertRouteResponseAsync(
            path,
            expectedBody,
            ("/library/{shelf}/archive/{**asset}", "Asset"),
            ("/library/{shelf}/archive/pages/{**page:nonfile}", "Page"),
            ("/library/local/archive/index", "Index"));
    }

    private static async Task AssertRouteResponseAsync(string path, string expectedBody, params (string Pattern, string Name)[] routes)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder => webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        foreach (var (pattern, name) in routes)
                        {
                            endpoints.Map(pattern, WriteRouteResponseAsync).WithDisplayName(name);
                        }
                    });
                }))
            .ConfigureServices(services => services.AddRouting())
            .Build();

        await host.StartAsync();
        using var client = host.GetTestClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }

    private static Task WriteRouteResponseAsync(HttpContext context)
    {
        var values = string.Join(",", context.Request.RouteValues
            .OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(value => $"{value.Key}={value.Value ?? "<null>"}"));

        return context.Response.WriteAsync($"{context.GetEndpoint().DisplayName}:{values}");
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
