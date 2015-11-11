// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Controllers
{
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
            activator.Setup(a => a.Create(context, typeof(MyController)))
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
            var factory = CreateControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

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
            var factory = CreateControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

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
            var factory = CreateControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

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
            var factory = CreateControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

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
            var factory = CreateControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateController(context));
            Assert.Equal(
                $"Unable to resolve service for type '{typeof(TestService).FullName}' while attempting to activate " +
                $"'{typeof(ControllerThatCannotBeActivated).FullName}'.", 
                exception.Message);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(OpenGenericType<>))]
        [InlineData(typeof(AbstractType))]
        [InlineData(typeof(InterfaceType))]
        public void CreateController_ThrowsIfControllerCannotBeActivated(Type type)
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = type.GetTypeInfo()
            };

            var context = new ControllerContext()
            {
                ActionDescriptor = actionDescriptor,
                HttpContext = new DefaultHttpContext()
                {
                    RequestServices = GetServices(),
                },
            };
            var factory = CreateControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateController(context));
            Assert.Equal(
                $"The type '{type.FullName}' cannot be activated by '{typeof(DefaultControllerFactory).FullName}' " +
                "because it is either a value type, an interface, an abstract class or an open generic type.",
                exception.Message);
        }

        [Fact]
        public void DefaultControllerFactory_DisposesIDisposableController()
        {
            // Arrange
            var factory = CreateControllerFactory();
            var controller = new MyController();

            // Act + Assert
            Assert.False(controller.Disposed);

            factory.ReleaseController(controller);

            Assert.True(controller.Disposed);
        }

        [Fact]
        public void DefaultControllerFactory_ReleasesNonIDisposableController()
        {
            // Arrange
            var factory = CreateControllerFactory();
            var controller = new object();

            // Act + Assert (does not throw)
            factory.ReleaseController(controller);
        }

        private IServiceProvider GetServices()
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IUrlHelper)))
                    .Returns(Mock.Of<IUrlHelper>());
            services.Setup(s => s.GetService(typeof(IModelMetadataProvider)))
                    .Returns(metadataProvider);
            services.Setup(s => s.GetService(typeof(IObjectModelValidator)))
                    .Returns(new DefaultObjectValidator(new IExcludeTypeValidationFilter[0], metadataProvider));
            return services.Object;
        }

        private static DefaultControllerFactory CreateControllerFactory(IControllerActivator controllerActivator = null)
        {
            controllerActivator = controllerActivator ?? Mock.Of<IControllerActivator>();
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

        private class Controller
        {
        }

        private class OpenGenericType<T> : Controller
        {
        }

        private abstract class AbstractType : Controller
        {
        }

        private interface InterfaceType
        {
        }
    }
}
