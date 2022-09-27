// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Controllers;

public class ControllerActivatorProviderTest
{
    [Fact]
    public void CreateActivator_InvokesIControllerActivator_IfItIsNotDefaultControllerActivator()
    {
        // Arrange
        var expected = new object();
        var activator = new Mock<IControllerActivator>();
        activator.Setup(a => a.Create(It.IsAny<ControllerContext>()))
            .Returns(expected)
            .Verifiable();
        var activatorProvider = new ControllerActivatorProvider(activator.Object);
        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(object).GetTypeInfo(),
        };

        // Act
        var activatorDelegate = activatorProvider.CreateActivator(descriptor);
        var result = activatorDelegate(new ControllerContext());

        // Assert
        Assert.Same(expected, result);
        activator.Verify();
    }

    [Fact]
    public void CreateActivator_ActivatesControllerInstance()
    {
        // Arrange
        var expected = new TestService();
        var activator = new DefaultControllerActivator(Mock.Of<ITypeActivatorCache>());
        var activatorProvider = new ControllerActivatorProvider(activator);
        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
        };
        var serviceProvider = new ServiceCollection()
            .AddSingleton(expected)
            .BuildServiceProvider();
        var context = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
            },
        };

        // Act
        var activatorDelegate = activatorProvider.CreateActivator(descriptor);
        var result = activatorDelegate(context);

        // Assert
        var actual = Assert.IsType<TestController>(result);
        Assert.Same(expected, actual.TestService);
    }

    [Fact]
    public void CreateReleaser_InvokesIControllerActivator_IfItIsNotDefaultControllerActivator()
    {
        // Arrange
        var expected = new object();
        var activator = new Mock<IControllerActivator>();
        activator.Setup(a => a.Release(It.IsAny<ControllerContext>(), expected))
            .Verifiable();
        var activatorProvider = new ControllerActivatorProvider(activator.Object);
        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(object).GetTypeInfo(),
        };

        // Act
        var releaseDelegate = activatorProvider.CreateReleaser(descriptor);
        releaseDelegate(new ControllerContext(), expected);

        // Assert
        activator.Verify();
    }

    [Fact]
    public void CreateReleaser_ReturnsNullIfControllerIsNotDisposable()
    {
        // Arrange
        var activator = new DefaultControllerActivator(Mock.Of<ITypeActivatorCache>());
        var activatorProvider = new ControllerActivatorProvider(activator);
        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
        };

        // Act
        var releaseDelegate = activatorProvider.CreateReleaser(descriptor);

        // Assert
        Assert.Null(releaseDelegate);
    }

    [Fact]
    public void CreateReleaser_ReturnsDelegateThatDisposesInstance()
    {
        // Arrange
        var activator = new DefaultControllerActivator(Mock.Of<ITypeActivatorCache>());
        var activatorProvider = new ControllerActivatorProvider(activator);
        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(DisposableController).GetTypeInfo(),
        };
        var controller = new DisposableController();

        // Act
        var releaseDelegate = activatorProvider.CreateReleaser(descriptor);

        // Assert
        Assert.NotNull(releaseDelegate);
        releaseDelegate(new ControllerContext(), controller);
        Assert.True(controller.Disposed);
    }

    private class TestController
    {
        public TestController(TestService testService)
        {
            TestService = testService;
        }

        public TestService TestService { get; }
    }

    private class DisposableController : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private class TestService
    {

    }
}
