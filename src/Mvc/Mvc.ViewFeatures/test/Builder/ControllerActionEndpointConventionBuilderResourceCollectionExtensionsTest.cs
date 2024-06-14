// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder;

public class ControllerActionEndpointConventionBuilderResourceCollectionExtensionsTest
{
    [Fact]
    public void WithStaticAssets_AddsEmptyResourceCollection_ToEndpoints_NoStaticAssetsMapped()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        var builder = endpointBuilder.MapControllers();

        // Act
        builder.WithStaticAssets();

        // Assert
        Assert.All(endpointBuilder.DataSources.First().Endpoints, e =>
        {
            var apiController = e.Metadata.GetMetadata<ApiControllerAttribute>();
            if (apiController != null)
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.Null(metadata);
            }
            else
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.NotNull(metadata);
                var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
                Assert.Equal(0, list.Count);
            }
        });
    }

    [Fact]
    public void WithStaticAssets_AddsEmptyResourceCollection_ToEndpoints_NoMatchingStaticAssetsMapped()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var builder = endpointBuilder.MapControllers();

        // Act
        builder.WithStaticAssets();

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(1).First().Endpoints, e =>
        {
            var apiController = e.Metadata.GetMetadata<ApiControllerAttribute>();
            if (apiController != null)
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.Null(metadata);
            }
            else
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.NotNull(metadata);
                var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
                Assert.Equal(0, list.Count);
            }
        });
    }

    [Fact]
    public void WithStaticAssets_AddsResourceCollection_ToEndpoints_NamedManifest()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var builder = endpointBuilder.MapControllers();

        // Act
        builder.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");

        // Assert
        var endpoints = endpointBuilder.DataSources.Skip(1).First().Endpoints;
        Assert.All(endpoints, e =>
        {
            var apiController = e.Metadata.GetMetadata<ApiControllerAttribute>();
            if (apiController != null)
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.Null(metadata);
            }
            else
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.NotNull(metadata);
                var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
                Assert.Equal(1, list.Count);
                Assert.Equal("named.css", list[0].Url);
            }
        });
    }

    [Fact]
    public void WithStaticAssets_AddsResourceCollection_ToEndpoints_DefaultManifest()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets();
        var builder = endpointBuilder.MapControllers();

        // Act
        builder.WithStaticAssets();

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(1).First().Endpoints, e =>
        {
            var apiController = e.Metadata.GetMetadata<ApiControllerAttribute>();
            if (apiController != null)
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.Null(metadata);
            }
            else
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.NotNull(metadata);
                var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
                Assert.Equal(1, list.Count);
                Assert.Equal("default.css", list[0].Url);
            }
        });
    }

    [Fact]
    public void WithStaticAssets_AddsDefaultResourceCollectionToEndpoints_WhenNoManifestProvided_EvenIfManyAvailable()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets();
        endpointBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var builder = endpointBuilder.MapControllers();

        // Act
        builder.WithStaticAssets();

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(2).First().Endpoints, e =>
        {
            var apiController = e.Metadata.GetMetadata<ApiControllerAttribute>();
            if (apiController != null)
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.Null(metadata);
            }
            else
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.NotNull(metadata);
                var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
                Assert.Equal(1, list.Count);
                Assert.Equal("default.css", list[0].Url);
            }
        });
    }

    [Fact]
    public void WithStaticAssets_AddsMatchingResourceCollectionToEndpoints_WhenExplicitManifestProvided_EvenIfManyAvailable()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets();
        endpointBuilder.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var builder = endpointBuilder.MapControllers();

        // Act
        builder.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");

        // Assert
        Assert.All(endpointBuilder.DataSources.Skip(2).First().Endpoints, e =>
        {
            var apiController = e.Metadata.GetMetadata<ApiControllerAttribute>();
            if (apiController != null)
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.Null(metadata);
            }
            else
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.NotNull(metadata);
                var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
                Assert.Equal(1, list.Count);
                Assert.Equal("named.css", list[0].Url);
            }
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
        var builder = group.MapControllers();

        // Act
        builder.WithStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");

        // Assert
        var groupEndpoints = Assert.IsAssignableFrom<IEndpointRouteBuilder>(group).DataSources;
        Assert.All(groupEndpoints.Skip(1).First().Endpoints, e =>
        {
            var apiController = e.Metadata.GetMetadata<ApiControllerAttribute>();
            if (apiController != null)
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.Null(metadata);
            }
            else
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.NotNull(metadata);
                var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
                Assert.Equal(1, list.Count);
                Assert.Equal("named.css", list[0].Url);
            }
        });
    }

    [Fact]
    public void WithStaticAssets_AddsEmptyCollectionFromGroup_WhenMappingNotFound_InsideGroup()
    {
        // Arrange
        var endpointBuilder = new TestEndpointRouteBuilder();
        endpointBuilder.MapStaticAssets();

        var group = endpointBuilder.MapGroup("/group");
        group.MapStaticAssets("TestManifests/Test.staticwebassets.endpoints.json");
        var builder = group.MapControllers();

        // Act
        builder.WithStaticAssets();

        // Assert
        var groupEndpoints = Assert.IsAssignableFrom<IEndpointRouteBuilder>(group).DataSources;
        Assert.All(groupEndpoints.Skip(1).First().Endpoints, e =>
        {
            var apiController = e.Metadata.GetMetadata<ApiControllerAttribute>();
            if (apiController != null)
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.Null(metadata);
            }
            else
            {
                var metadata = e.Metadata.GetMetadata<ResourceAssetCollection>();
                Assert.NotNull(metadata);
                var list = Assert.IsAssignableFrom<IReadOnlyList<ResourceAsset>>(metadata);
                Assert.Equal(0, list.Count);
            }
        });
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
            collection.AddSingleton(new ApplicationPartManager());
            collection.AddSingleton(new DiagnosticListener("Microsoft.AspNetCore"));
            collection.AddSingleton<DiagnosticSource>(new TestDiagnosticSource());
            collection.AddLogging();
            collection.AddOptions();
            collection.AddMvcCore()
                .ConfigureApplicationPartManager(apm =>
                {
                    apm.FeatureProviders.Clear();
                    apm.FeatureProviders.Add(new TestControllerFeatureProvider());
                });
            return collection.BuildServiceProvider();
        }

        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IApplicationBuilder CreateApplicationBuilder()
        {
            return _applicationBuilder.New();
        }

        private class TestControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
            {
                feature.Controllers.Clear();
                feature.Controllers.Add(typeof(TestController).GetTypeInfo());
                feature.Controllers.Add(typeof(MyApiController).GetTypeInfo());
            }
        }

        private class TestController : Controller
        {
            [HttpGet("/")]
            public void Index() { }
        }

        [ApiController]
        private class MyApiController : ControllerBase
        {
            [HttpGet("other")]
            public void Index() { }
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
}
