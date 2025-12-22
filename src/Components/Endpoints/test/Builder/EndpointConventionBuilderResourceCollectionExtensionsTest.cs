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
        group.MapGet("/endpoint", () => "test");

        // Get the endpoint from the data source created by the group
        var dataSource = Assert.Single(routeBuilder.DataSources);
        var endpoint = Assert.Single(dataSource.Endpoints);
        
        var metadata = endpoint.Metadata.GetMetadata<ResourceAssetCollection>();
        Assert.Null(metadata);
    }

    [Fact]
    public void WithStaticAssets_AddsResourceCollection_ToEndpoints_WithMatchingManifest()
    {
        var routeBuilder = new TestEndpointRouteBuilder();
        routeBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        
        var group = routeBuilder.MapGroup("/test");
        group.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        group.MapGet("/endpoint", () => "test");

        // Get the endpoint from the data source created by the group
        var dataSource = Assert.Single(routeBuilder.DataSources.Skip(1));
        var endpoint = Assert.Single(dataSource.Endpoints);
        
        var collection = endpoint.Metadata.GetMetadata<ResourceAssetCollection>();
        Assert.NotNull(collection);
        
        var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(collection);
        Assert.Single(list);
        Assert.Equal("named.css", list[0].Url);
        
        var preloadCollection = endpoint.Metadata.GetMetadata<ResourcePreloadCollection>();
        Assert.NotNull(preloadCollection);
        
        var importMap = endpoint.Metadata.GetMetadata<ImportMapDefinition>();
        Assert.NotNull(importMap);
    }

    [Fact]
    public void WithStaticAssets_DoesNotAddResourceCollection_WhenAlreadyExists()
    {
        var routeBuilder = new TestEndpointRouteBuilder();
        routeBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        
        var existingCollection = new ResourceAssetCollection([]);
        var group = routeBuilder.MapGroup("/test");
        ((IEndpointConventionBuilder)group).Add(eb => eb.Metadata.Add(existingCollection));
        group.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        group.MapGet("/endpoint", () => "test");

        // Get the endpoint from the data source created by the group
        var dataSource = Assert.Single(routeBuilder.DataSources.Skip(1));
        var endpoint = Assert.Single(dataSource.Endpoints);

        var collections = endpoint.Metadata.GetOrderedMetadata<ResourceAssetCollection>();
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
        group.MapGet("/endpoint", () => "test");

        // Get the endpoint from the data source created by the group
        var dataSource = Assert.Single(routeBuilder.DataSources.Skip(1));
        var endpoint = Assert.Single(dataSource.Endpoints);
        
        var collection = endpoint.Metadata.GetMetadata<ResourceAssetCollection>();
        Assert.NotNull(collection);
        
        var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(collection);
        Assert.Single(list);
        Assert.Equal("default.css", list[0].Url);
    }

    [Fact]
    public void WithStaticAssets_OnMapGet_AddsResourceCollection_WhenEndpointBuilderImplementsIEndpointRouteBuilder()
    {
        var routeBuilder = new TestEndpointRouteBuilder();
        routeBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        
        routeBuilder.MapGet("/endpoint", () => "test")
            .WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");

        // Get the endpoint from the data source created by MapGet
        var dataSource = Assert.Single(routeBuilder.DataSources.Skip(1));
        var endpoint = Assert.Single(dataSource.Endpoints);
        
        var collection = endpoint.Metadata.GetMetadata<ResourceAssetCollection>();
        Assert.NotNull(collection);
        
        var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(collection);
        Assert.Single(list);
        Assert.Equal("named.css", list[0].Url);
    }

    [Fact]
    public void WithStaticAssets_OnMapGetInGroup_AddsResourceCollection()
    {
        var routeBuilder = new TestEndpointRouteBuilder();
        routeBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        
        var group = routeBuilder.MapGroup("/test");
        group.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        group.MapGet("/endpoint", () => "test");

        // Get the endpoint from the data source created by the group
        var dataSource = Assert.Single(routeBuilder.DataSources.Skip(1));
        var endpoint = Assert.Single(dataSource.Endpoints);
        
        var collection = endpoint.Metadata.GetMetadata<ResourceAssetCollection>();
        Assert.NotNull(collection);
        
        var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(collection);
        Assert.Single(list);
        Assert.Equal("named.css", list[0].Url);
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