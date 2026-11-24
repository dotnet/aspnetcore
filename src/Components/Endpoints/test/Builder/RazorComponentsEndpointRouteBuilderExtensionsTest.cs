// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Web;
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
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task MapRazorComponents_ResolvesBrowserToolsHotReloadSettingsWithMultipleRoots(bool separateGroups, bool overlappingHosts, bool reverseOrder)
    {
        using var services = CreateServices();
        var application = CreateApplication(services, endpoints => MapRoots(endpoints, separateGroups, overlappingHosts, reverseOrder));
        var prefix = separateGroups ? "/shared" : "";

        foreach (var host in overlappingHosts ? new[] { "a.example", "b.example", "c.example" } : new[] { "a.example", "b.example" })
        {
            var context = CreateHttpContext(services, prefix + BrowserToolsHotReloadSettings.Path, host);

            await application(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
            Assert.Equal("no-store", context.Response.Headers.CacheControl);
            Assert.Equal("""{ "hotReload": false }""", Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
        }

        var unmatchedHost = CreateHttpContext(services, prefix + BrowserToolsHotReloadSettings.Path, "other.example");
        await application(unmatchedHost);
        Assert.Equal(StatusCodes.Status404NotFound, unmatchedHost.Response.StatusCode);

        var unsupportedMethod = CreateHttpContext(services, prefix + BrowserToolsHotReloadSettings.Path, "b.example");
        unsupportedMethod.Request.Method = HttpMethods.Post;
        await application(unsupportedMethod);
        Assert.Equal(StatusCodes.Status405MethodNotAllowed, unsupportedMethod.Response.StatusCode);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task MapRazorComponents_ResolvesOpaqueRedirectionWithMultipleRoots(bool separateGroups, bool overlappingHosts, bool reverseOrder)
    {
        using var services = CreateServices();
        var application = CreateApplication(services, endpoints => MapRoots(endpoints, separateGroups, overlappingHosts, reverseOrder));
        var prefix = separateGroups ? "/shared" : "";

        foreach (var host in overlappingHosts ? new[] { "a.example", "b.example", "c.example" } : new[] { "a.example", "b.example" })
        {
            var invalidRequest = CreateHttpContext(services, $"{prefix}/_framework/opaque-redirect?url=x", host);

            await application(invalidRequest);

            Assert.Equal(StatusCodes.Status400BadRequest, invalidRequest.Response.StatusCode);

            var redirectUrl = OpaqueRedirection.CreateProtectedRedirectionUrl(invalidRequest, "/destination");
            var validRequest = CreateHttpContext(services, $"{prefix}/{redirectUrl}", host);

            await application(validRequest);

            Assert.Equal(StatusCodes.Status302Found, validRequest.Response.StatusCode);
            Assert.Equal("/destination", validRequest.Response.Headers.Location);
        }

        var unmatchedHost = CreateHttpContext(services, $"{prefix}/_framework/opaque-redirect?url=x", "other.example");
        await application(unmatchedHost);
        Assert.Equal(StatusCodes.Status404NotFound, unmatchedHost.Response.StatusCode);
    }

    [Fact]
    public async Task MapRazorComponents_PreservesStaticAssetFallbackForLaterPageLessWebAssemblyRoot()
    {
        const string manifest = "TestManifests/Test.staticwebassets.endpoints.json";
        using var services = CreateServices();
        var application = CreateApplication(services, endpoints =>
        {
            endpoints.MapStaticAssets(manifest);

            // These roots are in a referenced assembly without pages, so no page endpoint can supply the resource collection.
            endpoints.MapRazorComponents<HeadOutlet>().WithStaticAssets(manifest);
            endpoints.MapRazorComponents<PageTitle>()
                .WithStaticAssets(manifest)
                .AddInteractiveWebAssemblyRenderMode(options =>
                {
                    options.PathPrefix = "/client";
                    options.StaticAssetsManifestPath = manifest;
                });

            endpoints.MapFallback(context => context.Response.WriteAsync("fallback"));
        });
        var context = CreateHttpContext(services, "/client/_framework/missing");

        await application(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("fallback", Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
    }

    [Fact]
    public async Task MapRazorComponents_PreservesDistinctPrefixesAndRoutingPriority()
    {
        using var services = CreateServices();
        var application = CreateApplication(services, endpoints =>
        {
            endpoints.MapGroup("/first").MapRazorComponents<FirstApp>();
            endpoints.MapGroup("/second").MapRazorComponents<SecondApp>();
            var unconstrained = endpoints.MapGroup("/unconstrained");
            unconstrained.MapRazorComponents<FirstApp>();
            unconstrained.MapRazorComponents<SecondApp>();
            endpoints.MapGroup("/priority").MapRazorComponents<FirstApp>();
            endpoints.MapGroup("/priority").MapRazorComponents<SecondApp>().Add(builder =>
            {
                ((RouteEndpointBuilder)builder).Order--;
                builder.Metadata.Add("preferred");
            });
        });

        foreach (var prefix in new[] { "/first", "/second", "/unconstrained", "/priority" })
        {
            foreach (var path in new[] { BrowserToolsHotReloadSettings.Path, "/_framework/opaque-redirect?url=x" })
            {
                var context = CreateHttpContext(services, prefix + path);
                await application(context);

                Assert.Equal(path == BrowserToolsHotReloadSettings.Path ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest, context.Response.StatusCode);
                if (prefix == "/priority")
                {
                    Assert.Equal("preferred", context.GetEndpoint()!.Metadata.GetMetadata<string>());
                }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MapRazorComponents_DoesNotResolveApplicationEndpointAmbiguity(bool replaceFrameworkHandler)
    {
        using var services = CreateServices();
        var application = CreateApplication(services, endpoints =>
        {
            endpoints.MapGroup("/shared").MapRazorComponents<FirstApp>();
            var second = endpoints.MapGroup("/shared").MapRazorComponents<SecondApp>();
            if (replaceFrameworkHandler)
            {
                second.Add(builder => builder.RequestDelegate = context => context.Response.WriteAsync("custom"));
            }
            else
            {
                endpoints.MapGet("/shared" + BrowserToolsHotReloadSettings.Path, () => "custom").WithOrder(int.MaxValue);
                endpoints.MapGet("/shared/_framework/opaque-redirect", () => "custom");
            }
        });

        foreach (var path in new[] { BrowserToolsHotReloadSettings.Path, "/_framework/opaque-redirect?url=x" })
        {
            var context = CreateHttpContext(services, "/shared" + path);
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => application(context));
            Assert.Equal("Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException", exception.GetType().FullName);
        }
    }

    private static void MapRoots(IEndpointRouteBuilder endpoints, bool separateGroups, bool overlappingHosts, bool reverseOrder)
    {
        IEndpointRouteBuilder first = separateGroups ? endpoints.MapGroup("/shared") : endpoints;
        IEndpointRouteBuilder second = separateGroups ? endpoints.MapGroup("/shared") : endpoints;
        string[] firstHosts = overlappingHosts ? ["a.example", "b.example"] : ["a.example"];
        string[] secondHosts = overlappingHosts ? ["b.example", "c.example"] : ["b.example"];
        if (reverseOrder)
        {
            second.MapRazorComponents<SecondApp>().RequireHost(secondHosts);
            first.MapRazorComponents<FirstApp>().RequireHost(firstHosts);
        }
        else
        {
            first.MapRazorComponents<FirstApp>().RequireHost(firstHosts);
            second.MapRazorComponents<SecondApp>().RequireHost(secondHosts);
        }
    }

    private static RequestDelegate CreateApplication(IServiceProvider services, Action<IEndpointRouteBuilder> configureEndpoints)
    {
        var application = new ApplicationBuilder(services);
        application.UseRouting();
        application.UseEndpoints(configureEndpoints);

        return application.Build();
    }

    private static ServiceProvider CreateServices()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(environment => environment.EnvironmentName).Returns(Environments.Development);
        environment.SetupGet(environment => environment.ApplicationName).Returns(typeof(FirstApp).Assembly.GetName().Name);
        environment.SetupGet(environment => environment.ContentRootPath).Returns(AppContext.BaseDirectory);
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
        services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider services, string path, string host = "localhost")
    {
        var uri = new Uri($"http://localhost{path}");
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Host = new HostString(host);
        context.Request.Path = uri.AbsolutePath;
        context.Request.QueryString = new QueryString(uri.Query);
        context.Response.Body = new MemoryStream();

        return context;
    }

    private sealed class FirstApp : ComponentBase;
    private sealed class SecondApp : ComponentBase;
}
