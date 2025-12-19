// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.Builder;

public class EndpointConventionBuilderResourceCollectionExtensionsTest
{
    [Fact]
    public void WithStaticAssets_DoesNotAddResourceCollection_ToEndpoints_NoStaticAssetsMapped()
    {
        var routeBuilder = new TestEndpointRouteBuilder();
        var group = routeBuilder.MapGroup("/test");

        group.WithStaticAssets();
        
        var endpointBuilder = new TestEndpointBuilder(routeBuilder);
        ApplyConventions(group, endpointBuilder);
        
        var metadata = endpointBuilder.Metadata.OfType<ResourceAssetCollection>().FirstOrDefault();
        Assert.Null(metadata);
    }

    [Fact]
    public void WithStaticAssets_AddsResourceCollection_ToEndpoints_WithMatchingManifest()
    {
        var routeBuilder = new TestEndpointRouteBuilder();
        routeBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var group = routeBuilder.MapGroup("/test");

        group.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");

        var endpointBuilder = new TestEndpointBuilder(routeBuilder);
        ApplyConventions(group, endpointBuilder);
        
        var collection = endpointBuilder.Metadata.OfType<ResourceAssetCollection>().FirstOrDefault();
        Assert.NotNull(collection);
        
        var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(collection);
        Assert.Single(list);
        Assert.Equal("named.css", list[0].Url);
        
        var preloadCollection = endpointBuilder.Metadata.OfType<ResourcePreloadCollection>().FirstOrDefault();
        Assert.NotNull(preloadCollection);
        
        var importMap = endpointBuilder.Metadata.OfType<ImportMapDefinition>().FirstOrDefault();
        Assert.NotNull(importMap);
    }

    [Fact]
    public void WithStaticAssets_DoesNotAddResourceCollection_WhenAlreadyExists()
    {
        var routeBuilder = new TestEndpointRouteBuilder();
        routeBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var group = routeBuilder.MapGroup("/test");
        
        var existingCollection = new ResourceAssetCollection([]);
        var endpointBuilder = new TestEndpointBuilder(routeBuilder);
        endpointBuilder.Metadata.Add(existingCollection);

        group.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        ApplyConventions(group, endpointBuilder);

        var collections = endpointBuilder.Metadata.OfType<ResourceAssetCollection>().ToList();
        Assert.Single(collections);
        Assert.Same(existingCollection, collections[0]);
    }

    [Fact]
    public void WithStaticAssets_AddsResourceCollection_ToEndpoints_DefaultManifest()
    {
        var routeBuilder = new TestEndpointRouteBuilder();
        routeBuilder.MapStaticAssets();
        var group = routeBuilder.MapGroup("/test");

        group.WithStaticAssets();

        var endpointBuilder = new TestEndpointBuilder(routeBuilder);
        ApplyConventions(group, endpointBuilder);
        
        var collection = endpointBuilder.Metadata.OfType<ResourceAssetCollection>().FirstOrDefault();
        Assert.NotNull(collection);
        
        var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(collection);
        Assert.Single(list);
        Assert.Equal("default.css", list[0].Url);
    }

    private static void ApplyConventions(RouteGroupBuilder group, EndpointBuilder endpointBuilder)
    {
        // Access conventions via reflection since they're private
        var conventionsField = typeof(RouteGroupBuilder).GetField("_conventions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var conventions = (List<Action<EndpointBuilder>>)conventionsField!.GetValue(group)!;
        
        foreach (var convention in conventions)
        {
            convention(endpointBuilder);
        }
    }

    // Test builder that implements both EndpointBuilder and IEndpointRouteBuilder
    private class TestEndpointBuilder : EndpointBuilder, IEndpointRouteBuilder
    {
        private readonly IEndpointRouteBuilder _routeBuilder;

        public TestEndpointBuilder(IEndpointRouteBuilder routeBuilder)
        {
            _routeBuilder = routeBuilder;
            ApplicationServices = TestEndpointRouteBuilder.CreateServiceProvider();
        }

        public override Endpoint Build()
        {
            throw new NotImplementedException();
        }

        public IServiceProvider ServiceProvider => _routeBuilder.ServiceProvider;
        public ICollection<EndpointDataSource> DataSources => _routeBuilder.DataSources;
        public IApplicationBuilder CreateApplicationBuilder() => _routeBuilder.CreateApplicationBuilder();
    }

    private class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        private readonly ApplicationBuilder _applicationBuilder;

        public TestEndpointRouteBuilder()
        {
            _applicationBuilder = new ApplicationBuilder(ServiceProvider);
        }

        public IServiceProvider ServiceProvider { get; } = CreateServiceProvider();

        public static IServiceProvider CreateServiceProvider()
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            collection.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
            collection.AddRazorComponents();
            return collection.BuildServiceProvider();
        }

        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IApplicationBuilder CreateApplicationBuilder()
        {
            return _applicationBuilder.New();
        }

        private class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string ApplicationName { get; set; } = "TestApplication";
            public string EnvironmentName { get; set; } = "TestEnvironment";
            public string WebRootPath { get; set; } = "";
            public IFileProvider WebRootFileProvider { get => ContentRootFileProvider; set { } }
            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
            public IFileProvider ContentRootFileProvider { get; set; } = CreateTestFileProvider();

            private static TestFileProvider CreateTestFileProvider()
            {
                var provider = new TestFileProvider();
                provider.AddFile("site.css", "body { color: red; }");
                return provider;
            }
        }
    }
}