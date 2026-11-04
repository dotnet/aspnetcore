// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class DefaultPageLoaderTest
{
    private readonly IOptions<RazorPagesOptions> RazorPagesOptions = Options.Create(new RazorPagesOptions { Conventions = new PageConventionCollection(Mock.Of<IServiceProvider>()) });
    private readonly IActionDescriptorCollectionProvider ActionDescriptorCollectionProvider;

    public DefaultPageLoaderTest()
    {
        var actionDescriptors = new ActionDescriptorCollection(Array.Empty<ActionDescriptor>(), 1);
        ActionDescriptorCollectionProvider = Mock.Of<IActionDescriptorCollectionProvider>(v => v.ActionDescriptors == actionDescriptors);
    }

    [Fact]
    public async Task LoadAsync_InvokesApplicationModelProviders()
    {
        // Arrange
        var descriptor = new PageActionDescriptor();

        var compilerProvider = GetCompilerProvider();

        var mvcOptions = Options.Create(new MvcOptions());
        var endpointFactory = new ActionEndpointFactory(Mock.Of<RoutePatternTransformer>(), Enumerable.Empty<IRequestDelegateFactory>(), Mock.Of<IServiceProvider>());

        var provider1 = new Mock<IPageApplicationModelProvider>();
        var provider2 = new Mock<IPageApplicationModelProvider>();

        var sequence = 0;
        var pageApplicationModel1 = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
        var pageApplicationModel2 = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());

        provider1.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Equal(0, sequence++);
                Assert.Null(c.PageApplicationModel);
                c.PageApplicationModel = pageApplicationModel1;
            })
            .Verifiable();

        provider2.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Equal(1, sequence++);
                Assert.Same(pageApplicationModel1, c.PageApplicationModel);
                c.PageApplicationModel = pageApplicationModel2;
            })
            .Verifiable();

        provider1.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Equal(3, sequence++);
                Assert.Same(pageApplicationModel2, c.PageApplicationModel);
            })
            .Verifiable();

        provider2.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Equal(2, sequence++);
                Assert.Same(pageApplicationModel2, c.PageApplicationModel);
            })
            .Verifiable();

        var providers = new[]
        {
                provider1.Object, provider2.Object
            };

        var loader = new DefaultPageLoader(
            providers,
            compilerProvider,
            endpointFactory,
            RazorPagesOptions,
            mvcOptions);

        // Act
        var result = await loader.LoadAsync(new PageActionDescriptor(), EndpointMetadataCollection.Empty);

        // Assert
        provider1.Verify();
        provider2.Verify();
    }

    [Fact]
    public async Task LoadAsync_CreatesEndpoint_WithRoute()
    {
        // Arrange
        var descriptor = new PageActionDescriptor()
        {
            AttributeRouteInfo = new AttributeRouteInfo()
            {
                Template = "/test",
            },
        };

        var transformer = new Mock<RoutePatternTransformer>();
        transformer
            .Setup(t => t.SubstituteRequiredValues(It.IsAny<RoutePattern>(), It.IsAny<object>()))
            .Returns<RoutePattern, object>((p, v) => p);

        var compilerProvider = GetCompilerProvider();

        var mvcOptions = Options.Create(new MvcOptions());
        var endpointFactory = new ActionEndpointFactory(transformer.Object, Enumerable.Empty<IRequestDelegateFactory>(), Mock.Of<IServiceProvider>());

        var provider = new Mock<IPageApplicationModelProvider>();

        var pageApplicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());

        provider.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Null(c.PageApplicationModel);
                c.PageApplicationModel = pageApplicationModel;
            })
            .Verifiable();

        var providers = new[]
        {
                provider.Object,
            };

        var loader = new DefaultPageLoader(
            providers,
            compilerProvider,
            endpointFactory,
            RazorPagesOptions,
            mvcOptions);

        // Act
        var result = await loader.LoadAsync(descriptor, EndpointMetadataCollection.Empty);

        // Assert
        Assert.NotNull(result.Endpoint);
    }

    [Fact]
    public async Task LoadAsync_InvokesApplicationModelProviders_WithTheRightOrder()
    {
        // Arrange
        var descriptor = new PageActionDescriptor();
        var compilerProvider = GetCompilerProvider();
        var mvcOptions = Options.Create(new MvcOptions());
        var endpointFactory = new ActionEndpointFactory(Mock.Of<RoutePatternTransformer>(), Enumerable.Empty<IRequestDelegateFactory>(), Mock.Of<IServiceProvider>());

        var provider1 = new Mock<IPageApplicationModelProvider>();
        provider1.SetupGet(p => p.Order).Returns(10);
        var provider2 = new Mock<IPageApplicationModelProvider>();
        provider2.SetupGet(p => p.Order).Returns(-5);

        var sequence = 0;
        provider1.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Equal(1, sequence++);
                c.PageApplicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            })
            .Verifiable();

        provider2.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Equal(0, sequence++);
            })
            .Verifiable();

        provider1.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Equal(2, sequence++);
            })
            .Verifiable();

        provider2.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Equal(3, sequence++);
            })
            .Verifiable();

        var providers = new[]
        {
                provider1.Object, provider2.Object
            };

        var loader = new DefaultPageLoader(
            providers,
            compilerProvider,
            endpointFactory,
            RazorPagesOptions,
            mvcOptions);

        // Act
        var result = await loader.LoadAsync(new PageActionDescriptor(), EndpointMetadataCollection.Empty);

        // Assert
        provider1.Verify();
        provider2.Verify();
    }

    [Fact]
    public async Task LoadAsync_CachesResults()
    {
        // Arrange
        var descriptor = new PageActionDescriptor()
        {
            AttributeRouteInfo = new AttributeRouteInfo()
            {
                Template = "/test",
            },
        };

        var transformer = new Mock<RoutePatternTransformer>();
        transformer
            .Setup(t => t.SubstituteRequiredValues(It.IsAny<RoutePattern>(), It.IsAny<object>()))
            .Returns<RoutePattern, object>((p, v) => p);

        var compilerProvider = GetCompilerProvider();

        var mvcOptions = Options.Create(new MvcOptions());
        var endpointFactory = new ActionEndpointFactory(transformer.Object, Enumerable.Empty<IRequestDelegateFactory>(), Mock.Of<IServiceProvider>());

        var provider = new Mock<IPageApplicationModelProvider>();

        var pageApplicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());

        provider.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Null(c.PageApplicationModel);
                c.PageApplicationModel = pageApplicationModel;
            })
            .Verifiable();

        var providers = new[]
        {
                provider.Object,
            };

        var loader = new DefaultPageLoader(
            providers,
            compilerProvider,
            endpointFactory,
            RazorPagesOptions,
            mvcOptions);

        // Act
        var result1 = await loader.LoadAsync(descriptor, EndpointMetadataCollection.Empty);
        var result2 = await loader.LoadAsync(descriptor, EndpointMetadataCollection.Empty);

        // Assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public async Task LoadAsync_IsUniquePerPageDescriptor()
    {
        // Arrange
        var descriptor = new PageActionDescriptor()
        {
            AttributeRouteInfo = new AttributeRouteInfo()
            {
                Template = "/test",
            },
        };

        var descriptor2 = new PageActionDescriptor()
        {
            AttributeRouteInfo = new AttributeRouteInfo()
            {
                Template = "/test",
            },
        };

        var transformer = new Mock<RoutePatternTransformer>();
        transformer
            .Setup(t => t.SubstituteRequiredValues(It.IsAny<RoutePattern>(), It.IsAny<object>()))
            .Returns<RoutePattern, object>((p, v) => p);

        var compilerProvider = GetCompilerProvider();

        var mvcOptions = Options.Create(new MvcOptions());
        var endpointFactory = new ActionEndpointFactory(transformer.Object, Enumerable.Empty<IRequestDelegateFactory>(), Mock.Of<IServiceProvider>());

        var provider = new Mock<IPageApplicationModelProvider>();

        provider.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
            .Callback((PageApplicationModelProviderContext c) =>
            {
                Assert.Null(c.PageApplicationModel);
                c.PageApplicationModel = new PageApplicationModel(c.ActionDescriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            })
            .Verifiable();

        var providers = new[]
        {
                provider.Object,
            };

        var loader = new DefaultPageLoader(
            providers,
            compilerProvider,
            endpointFactory,
            RazorPagesOptions,
            mvcOptions);

        // Act
        var result1 = await loader.LoadAsync(descriptor, EndpointMetadataCollection.Empty);
        var result2 = await loader.LoadAsync(descriptor2, EndpointMetadataCollection.Empty);

        // Assert
        Assert.NotSame(result1, result2);
    }

    [Fact]
    public async Task LoadAsync_CompiledPageActionDescriptor_ReturnsSelf()
    {
        // Arrange
        var mvcOptions = Options.Create(new MvcOptions());
        var endpointFactory = new ActionEndpointFactory(Mock.Of<RoutePatternTransformer>(), Enumerable.Empty<IRequestDelegateFactory>(), Mock.Of<IServiceProvider>());
        var loader = new DefaultPageLoader(
            new[] { Mock.Of<IPageApplicationModelProvider>() },
            Mock.Of<IViewCompilerProvider>(),
            endpointFactory,
            RazorPagesOptions,
            mvcOptions);
        var pageDescriptor = new CompiledPageActionDescriptor();

        // Act
        var result1 = await loader.LoadAsync(pageDescriptor, new EndpointMetadataCollection());
        var result2 = await loader.LoadAsync(pageDescriptor, new EndpointMetadataCollection());

        // Assert
        Assert.Same(pageDescriptor, result1);
        Assert.Same(pageDescriptor, result2);
    }

    private static IViewCompilerProvider GetCompilerProvider()
    {
        var compiledItem = TestRazorCompiledItem.CreateForView(typeof(object), "/Views/Index.cshtml");
        var descriptor = new CompiledViewDescriptor(compiledItem);
        var compiler = new Mock<IViewCompiler>();
        compiler.Setup(c => c.CompileAsync(It.IsAny<string>()))
            .ReturnsAsync(descriptor);
        var compilerProvider = new Mock<IViewCompilerProvider>();
        compilerProvider.Setup(p => p.GetCompiler())
            .Returns(compiler.Object);
        return compilerProvider.Object;
    }
}
