// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder;

public class RazorComponentsEndpointConventionBuilderExtensionsTest
{
    [Fact]
    public void WithStaticAssets_DoesNotAddResourceCollection_ToEndpoints_NoStaticAssetsMapped()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        var builder = CreateRazorComponentsAppBuilder(endpointBuilder);

        // Act
        builder.WithStaticAssets();

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(1).First().Endpoints, e =>
        {
            if (e.Metadata.GetMetadata<ComponentTypeMetadata>() == null)
            {
                return;
            }

            var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
            Assert.Null(metadata);
        });
    }

    [Fact]
    public void WithStaticAssets_DoesNotAddResourceCollection_ToEndpoints_NoMatchingStaticAssetsMapped()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var builder = CreateRazorComponentsAppBuilder(endpointBuilder);

        // Act
        builder.WithStaticAssets();

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(2).First().Endpoints, e =>
        {
            var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
            Assert.Null(metadata);
        });
    }

    [Fact]
    public void WithStaticAssets_AddsResourceCollection_ToEndpoints_NamedManifest()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var builder = CreateRazorComponentsAppBuilder(endpointBuilder);

        // Act
        builder.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(2).First().Endpoints, e =>
        {
            var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
            Assert.NotNull(metadata);
            var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
            Assert.Equal(1, list.Count);
            Assert.Equal("named.css", list[0].Url);
        });
    }

    [Fact]
    public void WithStaticAssets_AddsDefaultResourceCollection_ToEndpoints_ByDefault()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets();

        // Act
        var builder = CreateRazorComponentsAppBuilder(endpointBuilder);

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(2).First().Endpoints, e =>
        {
            var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
            Assert.NotNull(metadata);
            var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
            Assert.Equal(1, list.Count);
            Assert.Equal("default.css", list[0].Url);
        });
    }

    [Fact]
    public void WithStaticAssets_AddsResourceCollection_ToEndpoints_DefaultManifest()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets();
        var builder = CreateRazorComponentsAppBuilder(endpointBuilder);

        // Act
        builder.WithStaticAssets();

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(2).First().Endpoints, e =>
        {
            var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
            Assert.NotNull(metadata);
            var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
            Assert.Equal(1, list.Count);
            Assert.Equal("default.css", list[0].Url);
        });
    }

    [Fact]
    public void WithStaticAssets_AddsDefaultResourceCollectionToEndpoints_WhenNoManifestProvided_EvenIfManyAvailable()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets();
        endpointBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");

        // Act
        var builder = CreateRazorComponentsAppBuilder(endpointBuilder);

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(3).First().Endpoints, e =>
        {
            var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
            Assert.NotNull(metadata);
            var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
            Assert.Equal(1, list.Count);
            Assert.Equal("default.css", list[0].Url);
        });
    }

    [Fact]
    public void WithStaticAssets_AddsMatchingResourceCollectionToEndpoints_WhenExplicitManifestProvided_EvenIfManyAvailable()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets();
        endpointBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var builder = CreateRazorComponentsAppBuilder(endpointBuilder);

        // Act
        builder.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(3).First().Endpoints, e =>
        {
            var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
            Assert.NotNull(metadata);
            var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
            Assert.Equal(1, list.Count);
            Assert.Equal("named.css", list[0].Url);
        });
    }

    [Fact]
    public void WithStaticAssets_AddsCollectionFromGroup_WhenMappedInsideAnEndpointGroup()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets();

        var group = endpointBuilder.MapGroup("/group");
        group.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var builder = CreateRazorComponentsAppBuilder(group);

        // Act
        builder.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");

        // Assert
        var groupEndpoints = Assert.IsAssignableFrom<IEndpointRouteBuilder>(group).DataSources;
        Assert.All(groupEndpoints.Skip(2).First().Endpoints, e =>
        {
            var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
            Assert.NotNull(metadata);
            var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
            Assert.Equal(1, list.Count);
            Assert.Equal("named.css", list[0].Url);
        });
    }

    [Fact]
    public void WithStaticAssets_DoesNotAddResourceCollectionFromGroup_WhenMappingNotFound_InsideGroup()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets();

        var group = endpointBuilder.MapGroup("/group");
        group.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var builder = CreateRazorComponentsAppBuilder(group);

        // Act
        builder.WithStaticAssets();

        // Assert
        var groupEndpoints = Assert.IsAssignableFrom<IEndpointRouteBuilder>(group).DataSources;
        Assert.All(groupEndpoints.Skip(2).First().Endpoints, e =>
        {
            var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
            Assert.Null(metadata);
        });
    }

    private RazorComponentsEndpointConventionBuilder CreateRazorComponentsAppBuilder(IEndpointRouteBuilder endpointBuilder)
    {
        var builder = endpointBuilder.MapRazorComponents<App>();
        builder.ApplicationBuilder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "App",
            [new PageComponentBuilder {
                PageType = typeof(App),
                RouteTemplates = ["/"],
                AssemblyName = "App",
            }],
            []
        ));
        return builder;
    }

    private class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        private readonly ApplicationBuilder _applicationBuilder;

        public TestEndpointRouteBuilder()
        {
            _applicationBuilder = new ApplicationBuilder(ServiceProvider);
        }

        public IServiceProvider ServiceProvider { get; } = CreateServiceProvider();

        private static IServiceProvider CreateServiceProvider()
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

        private class TestDiagnosticSource : DiagnosticSource
        {
            public override bool IsEnabled(string name)
            {
                return false;
            }

            public override void Write(string name, object value) { }
        }
    }

    private class App : IComponent
    {
        void IComponent.Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        Task IComponent.SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }
}
