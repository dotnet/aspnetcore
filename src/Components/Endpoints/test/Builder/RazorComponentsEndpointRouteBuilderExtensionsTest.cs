// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Microsoft.AspNetCore.Builder;

public class RazorComponentsEndpointRouteBuilderExtensionsTest
{
    [Fact]
    public void MapRazorComponents_SharesFrameworkEndpointsWithStaticAssets()
    {
        using var services = CreateServices();
        var application = new ApplicationBuilder(services);
        RazorComponentsEndpointConventionBuilder firstBuilder = null;
        RazorComponentsEndpointConventionBuilder secondBuilder = null;
        ICollection<EndpointDataSource> endpointDataSources = null;
        application.UseRouting();
        application.UseEndpoints(endpoints =>
        {
            endpointDataSources = endpoints.DataSources;
            const string manifest = "TestApplication.staticwebassets.endpoints.json";
            var group = endpoints.MapGroup("/group");
            group.MapStaticAssets(manifest);
            firstBuilder = group.MapRazorComponents<FirstApp>().WithStaticAssets(manifest)
                .RequireHost("a.example");
            firstBuilder.WithMetadata(new AuthorizeAttribute("Policy")).Add(RewriteFrameworkRoute);
            firstBuilder.AddRenderMode(new InteractiveWebAssemblyRenderMode());
            var collection = Assert.Single(((IEndpointRouteBuilder)group).DataSources
                .OfType<RazorComponentEndpointDataSourceCollection>());
            using var composite = new CompositeEndpointDataSource([collection]);
            Assert.Equal(2, composite.Endpoints.Count(endpoint =>
                endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>() is not null));
            var changeToken = composite.GetChangeToken();

            secondBuilder = group.MapRazorComponents<SecondApp>().WithStaticAssets(manifest)
                .RequireHost("a.example");
            secondBuilder.WithMetadata(new AuthorizeAttribute("Policy")).Add(RewriteFrameworkRoute);
            secondBuilder.AddRenderMode(new InteractiveWebAssemblyRenderMode());

            Assert.True(changeToken.HasChanged);

            var registered = endpoints.DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().ToArray();
            AssertFrameworkMetadata(Assert.Single(registered,
                endpoint => endpoint.RoutePattern.RawText == "/group/custom" + BrowserToolsHotReloadSettings.Path));
            AssertFrameworkMetadata(Assert.Single(registered,
                endpoint => endpoint.RoutePattern.RawText == "/group/custom/_framework/opaque-redirect"));
        });
        firstBuilder.WithStaticAssets("unmapped.json");
        secondBuilder.WithStaticAssets("unmapped.json");

        var registered = endpointDataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>() is not null);
        Assert.All(registered, endpoint => Assert.Null(endpoint.Metadata.GetMetadata<ResourceAssetCollection>()));
        Assert.All(registered, endpoint => Assert.Null(endpoint.Metadata.GetMetadata<ResourcePreloadCollection>()));
        Assert.All(registered, endpoint => Assert.Null(endpoint.Metadata.GetMetadata<ResourceCollectionUrlMetadata>()));
        Assert.All(registered, endpoint => Assert.Null(endpoint.Metadata.GetMetadata<ImportMapDefinition>()));
    }

    [Fact]
    public async Task MapRazorComponents_DoesNotChooseBetweenDifferentRootMetadata()
    {
        using var services = CreateServices();
        var firstMetadata = new RootMetadata("first");
        var secondMetadata = new RootMetadata("second");
        var application = new ApplicationBuilder(services);
        application.UseRouting();
        application.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorComponents<FirstApp>().WithMetadata(firstMetadata).Add(RewriteFrameworkRoute);
            endpoints.MapRazorComponents<SecondApp>().WithMetadata(secondMetadata).Add(RewriteFrameworkRoute);

            foreach (var frameworkPath in new[] { BrowserToolsHotReloadSettings.Path, "/_framework/opaque-redirect" })
            {
                var rewrittenPath = "/custom" + frameworkPath;
                var registered = endpoints.DataSources.SelectMany(source => source.Endpoints)
                    .OfType<RouteEndpoint>().Where(endpoint => endpoint.RoutePattern.RawText == rewrittenPath).ToArray();
                Assert.Equal(2, registered.Length);
                Assert.Same(firstMetadata, registered[0].Metadata.GetRequiredMetadata<RootMetadata>());
                Assert.Same(secondMetadata, registered[1].Metadata.GetRequiredMetadata<RootMetadata>());
                Assert.All(registered, endpoint =>
                    Assert.Equal(rewrittenPath, endpoint.Metadata.GetRequiredMetadata<IRouteDiagnosticsMetadata>().Route));
            }
        });
        var pipeline = application.Build();

        foreach (var frameworkPath in new[] { BrowserToolsHotReloadSettings.Path, "/_framework/opaque-redirect" })
        {
            var context = CreateHttpContext(services, "/custom" + frameworkPath, "localhost");

            var exception = await Record.ExceptionAsync(() => pipeline(context));

            Assert.NotNull(exception);
            Assert.Equal("Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException", exception.GetType().FullName);
        }

        var customHostApplication = new ApplicationBuilder(services);
        customHostApplication.UseRouting();
        customHostApplication.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorComponents<FirstApp>()
                .WithMetadata(new CustomHostMetadata(Array.Empty<string>(), suppressMatching: true));
            endpoints.MapRazorComponents<SecondApp>()
                .WithMetadata(new CustomHostMetadata(Array.Empty<string>(), suppressMatching: false));
        });
        pipeline = customHostApplication.Build();
        var customHostContext = CreateHttpContext(services, BrowserToolsHotReloadSettings.Path, "localhost");

        await pipeline(customHostContext);

        Assert.Equal(StatusCodes.Status200OK, customHostContext.Response.StatusCode);
    }

    private static void RewriteFrameworkRoute(EndpointBuilder endpointBuilder)
    {
        if (endpointBuilder is RouteEndpointBuilder routeEndpointBuilder &&
            endpointBuilder.Metadata.OfType<BlazorWebEndpointMetadata>().SingleOrDefault() is { } metadata)
        {
            routeEndpointBuilder.RoutePattern = RoutePatternFactory.Parse("/custom" + metadata.RoutePattern.RawText);
        }
    }

    private static void AssertFrameworkMetadata(RouteEndpoint endpoint)
    {
        var assets = endpoint.Metadata.GetRequiredMetadata<ResourceAssetCollection>();
        Assert.Equal("default.css", Assert.Single((IReadOnlyList<ResourceAsset>)assets).Url);
        Assert.NotNull(endpoint.Metadata.GetMetadata<ResourcePreloadCollection>());
        Assert.NotNull(endpoint.Metadata.GetMetadata<ResourceCollectionUrlMetadata>());
        Assert.NotNull(endpoint.Metadata.GetMetadata<ImportMapDefinition>());
    }

    private static ServiceProvider CreateServices()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(environment => environment.EnvironmentName).Returns(Environments.Development);
        environment.SetupGet(environment => environment.ApplicationName).Returns(typeof(FirstApp).Assembly.GetName().Name);
        environment.SetupGet(environment => environment.ContentRootPath).Returns(Directory.GetCurrentDirectory());
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
        var provider = new Mock<RenderModeEndpointProvider>();
        provider.Setup(provider => provider.Supports(It.IsAny<IComponentRenderMode>())).Returns(true);
        provider.Setup(provider => provider.GetEndpointBuilders(It.IsAny<IComponentRenderMode>(), It.IsAny<IApplicationBuilder>()))
            .Returns(Array.Empty<RouteEndpointBuilder>());
        services.AddSingleton(provider.Object);
        services.AddDataProtection().UseEphemeralDataProtectionProvider();

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider services, string path, string host)
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

    private sealed class RootMetadata(string routeParameter)
    {
        public string RouteParameter { get; } = routeParameter;
    }

    private sealed class CustomHostMetadata(string[] hosts, bool suppressMatching) : IHostMetadata, ISuppressMatchingMetadata
    {
        public IReadOnlyList<string> Hosts { get; } = hosts;

        public bool SuppressMatching { get; } = suppressMatching;
    }
}
