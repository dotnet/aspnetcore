// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Microsoft.AspNetCore.Builder;

public class RazorComponentsEndpointRouteBuilderExtensionsTest
{
    [Fact]
    public async Task MapRazorComponents_MapsBrowserToolsHotReloadSettingsOnceWithMultipleRoots()
    {
        using var services = CreateServices();
        var application = CreateApplication(services);
        var context = CreateHttpContext(services, BrowserToolsHotReloadSettings.Path);

        await application(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Equal("no-store", context.Response.Headers.CacheControl);
        Assert.Equal("""{ "hotReload": false }""", Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));

        var dataSource = services.GetRequiredService<EndpointDataSource>();
        Assert.Single(dataSource.Endpoints, endpoint =>
            endpoint is RouteEndpoint route && route.RoutePattern.RawText == BrowserToolsHotReloadSettings.Path);
    }

    [Fact]
    public async Task MapRazorComponents_MapsOpaqueRedirectionOnceWithMultipleRoots()
    {
        using var services = CreateServices();
        var application = CreateApplication(services);
        var invalidRequest = CreateHttpContext(services, "/_framework/opaque-redirect?url=x");

        await application(invalidRequest);

        Assert.Equal(StatusCodes.Status400BadRequest, invalidRequest.Response.StatusCode);

        var redirectUrl = OpaqueRedirection.CreateProtectedRedirectionUrl(invalidRequest, "/destination");
        var validRequest = CreateHttpContext(services, $"/{redirectUrl}");

        await application(validRequest);

        Assert.Equal(StatusCodes.Status302Found, validRequest.Response.StatusCode);
        Assert.Equal("/destination", validRequest.Response.Headers.Location);

        var dataSource = services.GetRequiredService<EndpointDataSource>();
        Assert.Single(dataSource.Endpoints, endpoint =>
            endpoint is RouteEndpoint route && route.RoutePattern.RawText == "/_framework/opaque-redirect");
    }

    private static RequestDelegate CreateApplication(IServiceProvider services)
    {
        var application = new ApplicationBuilder(services);
        application.UseRouting();
        application.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorComponents<FirstApp>();
            endpoints.MapRazorComponents<SecondApp>();
        });

        return application.Build();
    }

    private static ServiceProvider CreateServices()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(environment => environment.EnvironmentName).Returns(Environments.Development);
        environment.SetupGet(environment => environment.ApplicationName).Returns(typeof(FirstApp).Assembly.GetName().Name);
        environment.SetupGet(environment => environment.ContentRootFileProvider).Returns(new NullFileProvider());
        environment.SetupGet(environment => environment.WebRootFileProvider).Returns(new NullFileProvider());

        var services = new ServiceCollection();
        services.AddSingleton(environment.Object);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["DOTNET_WATCH"] = "0",
        }).Build());
        services.AddSingleton(new DiagnosticListener("Test"));
        services.AddRouting();
        services.AddRazorComponents();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider services, string path)
    {
        var uri = new Uri($"http://localhost{path}");
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = uri.AbsolutePath;
        context.Request.QueryString = new QueryString(uri.Query);
        context.Response.Body = new MemoryStream();

        return context;
    }

    private sealed class FirstApp : ComponentBase;
    private sealed class SecondApp : ComponentBase;
}
