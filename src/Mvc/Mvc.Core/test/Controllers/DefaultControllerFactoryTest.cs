// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Controllers;

public class DefaultControllerFactoryTest
{
    [Fact]
    public void CreateController_UsesControllerActivatorToInstantiateController()
    {
        // Arrange
        var expected = new MyController();
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(MyController).GetTypeInfo()
        };

        var context = new ControllerContext()
        {
            ActionDescriptor = actionDescriptor,
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = GetServices(),
            },
        };

        var activator = new Mock<IControllerActivator>();
        activator.Setup(a => a.Create(context))
                 .Returns(expected)
                 .Verifiable();

        var controllerFactory = CreateControllerFactory(activator.Object);

        // Act
        var result = controllerFactory.CreateController(context);

        // Assert
        var controller = Assert.IsType<MyController>(result);
        Assert.Same(expected, controller);
        activator.Verify();
    }

    [Fact]
    public void CreateController_SetsPropertiesFromActionContextHierarchy()
    {
        // Arrange
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(ControllerWithAttributes).GetTypeInfo()
        };

        var context = new ControllerContext()
        {
            ActionDescriptor = actionDescriptor,
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = GetServices(),
            },
        };
        var factory = CreateControllerFactory(new DefaultControllerActivator(new TypeActivatorCache()));

        // Act
        var result = factory.CreateController(context);

        // Assert
        var controller = Assert.IsType<ControllerWithAttributes>(result);
        Assert.Same(context, controller.ActionContext);
    }

    [Fact]
    public void CreateController_SetsControllerContext()
    {
        // Arrange
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(ControllerWithAttributes).GetTypeInfo()
        };

        var context = new ControllerContext()
        {
            ActionDescriptor = actionDescriptor,
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = GetServices(),
            },
        };
        var factory = CreateControllerFactory(new DefaultControllerActivator(new TypeActivatorCache()));

        // Act
        var result = factory.CreateController(context);

        // Assert
        var controller = Assert.IsType<ControllerWithAttributes>(result);
        Assert.Same(context, controller.ControllerContext);
    }

    [Fact]
    public void CreateController_IgnoresPropertiesThatAreNotDecoratedWithAttribute()
    {
        // Arrange
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(ControllerWithoutAttributes).GetTypeInfo()
        };

        var context = new ControllerContext()
        {
            ActionDescriptor = actionDescriptor,
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = GetServices(),
            },
        };
        var factory = CreateControllerFactory(new DefaultControllerActivator(new TypeActivatorCache()));

        // Act
        var result = factory.CreateController(context);

        // Assert
        var controller = Assert.IsType<ControllerWithoutAttributes>(result);
        Assert.Null(controller.ActionContext);
    }

    [Fact]
    public void CreateController_IgnoresNonPublicProperties()
    {
        // Arrange
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(ControllerWithNonVisibleProperties).GetTypeInfo()
        };

        var context = new ControllerContext()
        {
            ActionDescriptor = actionDescriptor,
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = GetServices(),
            },
        };
        var factory = CreateControllerFactory(new DefaultControllerActivator(new TypeActivatorCache()));

        // Act
        var result = factory.CreateController(context);

        // Assert
        var controller = Assert.IsType<ControllerWithNonVisibleProperties>(result);
        Assert.Null(controller.ActionContext);
        Assert.Null(controller.ControllerContext);
    }

    [Fact]
    public void CreateController_ThrowsIConstructorCannotBeActivated()
    {
        // Arrange
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(ControllerThatCannotBeActivated).GetTypeInfo()
        };

        var context = new ControllerContext()
        {
            ActionDescriptor = actionDescriptor,
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = GetServices(),
            },
        };
        var factory = CreateControllerFactory(new DefaultControllerActivator(new TypeActivatorCache()));

        // Act and Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateController(context));
        Assert.Equal(
            $"Unable to resolve service for type '{typeof(TestService).FullName}' while attempting to activate " +
            $"'{typeof(ControllerThatCannotBeActivated).FullName}'.",
            exception.Message);
    }

    [Fact]
    public void DefaultControllerFactory_DelegatesDisposalToControllerActivator()
    {
        // Arrange
        var activatorMock = new Mock<IControllerActivator>();
        activatorMock.Setup(s => s.Release(It.IsAny<ControllerContext>(), It.IsAny<object>()));

        var factory = CreateControllerFactory(activatorMock.Object);
        var controller = new MyController();

        // Act + Assert
        factory.ReleaseController(new ControllerContext(), controller);

        activatorMock.Verify();
    }

    [Fact]
    public async Task DefaultControllerFactory_DelegatesAsyncDisposalToControllerActivatorAsync()
    {
        // Arrange
        var activatorMock = new Mock<IControllerActivator>();
        activatorMock.Setup(s => s.Release(It.IsAny<ControllerContext>(), It.IsAny<object>()));

        var factory = CreateControllerFactory(activatorMock.Object);
        var controller = new MyAsyncDisposableController();

        // Act + Assert
        await factory.ReleaseControllerAsync(new ControllerContext(), controller);

        activatorMock.Verify();
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
            .Returns(new DefaultObjectValidator(
                metadataProvider,
                TestModelValidatorProvider.CreateDefaultProvider().ValidatorProviders,
                new MvcOptions()));
        return services.Object;
    }

    private static DefaultControllerFactory CreateControllerFactory(IControllerActivator controllerActivator = null)
    {
        var activatorMock = new Mock<IControllerActivator>();

        controllerActivator = controllerActivator ?? activatorMock.Object;
        var propertyActivators = new IControllerPropertyActivator[]
        {
                new DefaultControllerPropertyActivator(),
        };

        return new DefaultControllerFactory(controllerActivator, propertyActivators);
    }

    private class ControllerWithoutAttributes
    {
        public ActionContext ActionContext { get; set; }

        public ControllerContext ControllerContext { get; set; }
    }

    public class ControllerWithNonVisibleProperties
    {
        internal ActionContext ActionContext { get; set; }

        public ControllerContext ControllerContext { get; private set; }
    }

    private class ControllerWithAttributes
    {
        [ActionContext]
        public ActionContext ActionContext { get; set; }

        [ControllerContext]
        public ControllerContext ControllerContext { get; set; }
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

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return default;
        }
    }

    private class ControllerThatCannotBeActivated
    {
        public ControllerThatCannotBeActivated(TestService service)
        {
            Service = service;
        }

        public TestService Service { get; }
    }

    private class TestService
    {
    }
}
