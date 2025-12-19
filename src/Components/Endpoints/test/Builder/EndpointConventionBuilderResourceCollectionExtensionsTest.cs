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
        // Arrange
        var routeBuilder = new TestEndpointRouteBuilder();
        var conventionBuilder = new TestRouteGroupBuilder(routeBuilder);

        // Act
        conventionBuilder.WithStaticAssets();

        // Assert
        var endpointBuilderInstance = new TestEndpointBuilder();
        conventionBuilder.ApplyConventions(endpointBuilderInstance);
        
        var metadata = endpointBuilderInstance.Metadata.OfType<ResourceAssetCollection>().FirstOrDefault();
        Assert.Null(metadata);
    }

    [Fact]
    public void WithStaticAssets_AddsResourceCollection_ToEndpoints_WithMatchingManifest()
    {
        // Arrange
        var routeBuilder = new TestEndpointRouteBuilder();
        routeBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var conventionBuilder = new TestRouteGroupBuilder(routeBuilder);

        // Act
        conventionBuilder.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");

        // Assert
        var endpointBuilderInstance = new TestEndpointBuilder();
        conventionBuilder.ApplyConventions(endpointBuilderInstance);
        
        var collection = endpointBuilderInstance.Metadata.OfType<ResourceAssetCollection>().FirstOrDefault();
        Assert.NotNull(collection);
        
        var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(collection);
        Assert.Single(list);
        Assert.Equal("named.css", list[0].Url);
        
        // Verify other metadata is also added
        var preloadCollection = endpointBuilderInstance.Metadata.OfType<ResourcePreloadCollection>().FirstOrDefault();
        Assert.NotNull(preloadCollection);
        
        var importMap = endpointBuilderInstance.Metadata.OfType<ImportMapDefinition>().FirstOrDefault();
        Assert.NotNull(importMap);
    }

    [Fact]
    public void WithStaticAssets_DoesNotAddResourceCollection_WhenAlreadyExists()
    {
        // Arrange
        var routeBuilder = new TestEndpointRouteBuilder();
        routeBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var conventionBuilder = new TestRouteGroupBuilder(routeBuilder);
        
        var existingCollection = new ResourceAssetCollection([]);
        var endpointBuilderInstance = new TestEndpointBuilder();
        endpointBuilderInstance.Metadata.Add(existingCollection);

        // Act
        conventionBuilder.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        conventionBuilder.ApplyConventions(endpointBuilderInstance);

        // Assert
        var collections = endpointBuilderInstance.Metadata.OfType<ResourceAssetCollection>().ToList();
        Assert.Single(collections);
        Assert.Same(existingCollection, collections[0]);
    }

    [Fact]
    public void WithStaticAssets_AddsResourceCollection_ToEndpoints_DefaultManifest()
    {
        // Arrange
        var routeBuilder = new TestEndpointRouteBuilder();
        routeBuilder.MapStaticAssets();
        var conventionBuilder = new TestRouteGroupBuilder(routeBuilder);

        // Act
        conventionBuilder.WithStaticAssets();

        // Assert
        var endpointBuilderInstance = new TestEndpointBuilder();
        conventionBuilder.ApplyConventions(endpointBuilderInstance);
        
        var collection = endpointBuilderInstance.Metadata.OfType<ResourceAssetCollection>().FirstOrDefault();
        Assert.NotNull(collection);
        
        var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(collection);
        Assert.Single(list);
        Assert.Equal("default.css", list[0].Url);
    }

    // Test builder that implements both IEndpointConventionBuilder and IEndpointRouteBuilder
    private class TestRouteGroupBuilder : IEndpointConventionBuilder, IEndpointRouteBuilder
    {
        private readonly TestEndpointRouteBuilder _routeBuilder;
        private readonly List<Action<EndpointBuilder>> _conventions = [];

        public TestRouteGroupBuilder(TestEndpointRouteBuilder routeBuilder)
        {
            _routeBuilder = routeBuilder;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            ArgumentNullException.ThrowIfNull(convention);
            _conventions.Add(convention);
        }

        public void ApplyConventions(EndpointBuilder endpointBuilder)
        {
            foreach (var convention in _conventions)
            {
                convention(endpointBuilder);
            }
        }

        public IServiceProvider ServiceProvider => _routeBuilder.ServiceProvider;
        public ICollection<EndpointDataSource> DataSources => _routeBuilder.DataSources;
        public IApplicationBuilder CreateApplicationBuilder() => _routeBuilder.CreateApplicationBuilder();
    }

    private class TestEndpointBuilder : EndpointBuilder
    {
        public TestEndpointBuilder()
        {
            ApplicationServices = TestEndpointRouteBuilder.CreateServiceProvider();
        }

        public override Endpoint Build()
        {
            throw new NotImplementedException();
        }
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