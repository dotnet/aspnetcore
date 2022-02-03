// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Controllers;

public class DefaultControllerActivatorTest
{
    [Theory]
    [InlineData(typeof(TypeDerivingFromController))]
    [InlineData(typeof(PocoType))]
    public void Create_CreatesInstancesOfTypes(Type type)
    {
        // Arrange
        var activator = new DefaultControllerActivator(new TypeActivatorCache());
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider.Object
        };

        var context = new ControllerContext(
            new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor
                {
                    ControllerTypeInfo = type.GetTypeInfo()
                }));

        // Act
        var instance = activator.Create(context);

        // Assert
        Assert.IsType(type, instance);
    }

    [Fact]
    public void Release_DisposesController_IfDisposable()
    {
        // Arrange
        var controller = new MyController();
        var activator = new DefaultControllerActivator(Mock.Of<ITypeActivatorCache>());

        // Act
        activator.Release(new ControllerContext(), controller);

        // Assert
        Assert.True(controller.Disposed);
    }

    [Fact]
    public async Task ReleaseAsync_AsynchronouslyDisposesController_IfAsyncDisposableAsync()
    {
        // Arrange
        var controller = new MyAsyncDisposableController();
        var activator = new DefaultControllerActivator(Mock.Of<ITypeActivatorCache>());

        // Act
        await activator.ReleaseAsync(new ControllerContext(), controller);

        // Assert
        Assert.True(controller.Disposed);
    }

    [Fact]
    public async Task ReleaseAsync_SynchronouslyDisposesController_IfDisposableAsync()
    {
        // Arrange
        var controller = new MyController();
        var activator = new DefaultControllerActivator(Mock.Of<ITypeActivatorCache>());

        // Act
        await activator.ReleaseAsync(new ControllerContext(), controller);

        // Assert
        Assert.True(controller.Disposed);
    }

    [Fact]
    public async Task ReleaseAsync_SynchronouslyDisposesController_PrefersDisposeAsyncOverDispose()
    {
        // Arrange
        var controller = new MyDisposableAndAsyncDisposableController();
        var activator = new DefaultControllerActivator(Mock.Of<ITypeActivatorCache>());

        // Act
        await activator.ReleaseAsync(new ControllerContext(), controller);

        // Assert
        Assert.False(controller.SyncDisposed);
        Assert.True(controller.AsyncDisposed);
    }

    [Fact]
    public void DefaultControllerActivator_ReleasesNonIDisposableController()
    {
        // Arrange
        var activator = new DefaultControllerActivator(Mock.Of<ITypeActivatorCache>());
        var controller = new object();

        // Act + Assert (does not throw)
        activator.Release(Mock.Of<ControllerContext>(), controller);
    }

    [Fact]
    public void Create_TypeActivatesTypesWithServices()
    {
        // Arrange
        var activator = new DefaultControllerActivator(new TypeActivatorCache());
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        var testService = new TestService();
        serviceProvider.Setup(s => s.GetService(typeof(TestService)))
                       .Returns(testService)
                       .Verifiable();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider.Object
        };

        var context = new ControllerContext(
            new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor
                {
                    ControllerTypeInfo = typeof(TypeDerivingFromControllerWithServices).GetTypeInfo()
                }));

        // Act
        var instance = activator.Create(context);

        // Assert
        var controller = Assert.IsType<TypeDerivingFromControllerWithServices>(instance);
        Assert.Same(testService, controller.TestService);
        serviceProvider.Verify();
    }

    public class Controller
    {
    }

    private class TypeDerivingFromController : Controller
    {
    }

    private class TypeDerivingFromControllerWithServices : Controller
    {
        public TypeDerivingFromControllerWithServices(TestService service)
        {
            TestService = service;
        }

        public TestService TestService { get; }
    }

    private IServiceProvider GetServices()
    {
        var metadataProvider = new EmptyModelMetadataProvider();
        var services = new Mock<IServiceProvider>();
        services
            .Setup(s => s.GetService(typeof(IUrlHelper)))
            .Returns(Mock.Of<IUrlHelper>());
        services
            .Setup(s => s.GetService(typeof(IModelMetadataProvider)))
            .Returns(metadataProvider);
        services
            .Setup(s => s.GetService(typeof(IObjectModelValidator)))
            .Returns(new DefaultObjectValidator(metadataProvider, new List<IModelValidatorProvider>(), new MvcOptions()));
        return services.Object;
    }

    private class PocoType
    {
    }

    private class TestService
    {
    }

    private class MyController : IDisposable
    {
        public bool Disposed { get; set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private class MyAsyncDisposableController : IAsyncDisposable
    {
        public bool Disposed { get; set; }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return default;
        }
    }

    private class MyDisposableAndAsyncDisposableController : IDisposable, IAsyncDisposable
    {
        public bool AsyncDisposed { get; set; }
        public bool SyncDisposed { get; set; }

        public void Dispose()
        {
            SyncDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return default;
        }
    }
}
