// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class DefaultControllerFactoryTest
    {
        [Fact]
        public void CreateController_ThrowsIfActionDescriptorIsNotControllerActionDescriptor()
        {
            // Arrange
            var expected = "The action descriptor must be of type 'Microsoft.AspNet.Mvc.ControllerActionDescriptor'." +
                            Environment.NewLine + "Parameter name: actionContext";
            var actionDescriptor = new ActionDescriptor();
            var controllerFactory = new DefaultControllerFactory(Mock.Of<IControllerActivator>());
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext,
                                                  new RouteData(),
                                                  actionDescriptor);

            // Act and Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                        controllerFactory.CreateController(actionContext));
            Assert.Equal(expected, ex.Message);
            Assert.Equal("actionContext", ex.ParamName);
        }

        [Fact]
        public void CreateController_UsesControllerActivatorToInstantiateController()
        {
            // Arrange
            var expected = new MyController();
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(MyController).GetTypeInfo()
            };
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = GetServices();
            var actionContext = new ActionContext(httpContext,
                                                  new RouteData(),
                                                  actionDescriptor);
            var activator = new Mock<IControllerActivator>();
            activator.Setup(a => a.Create(actionContext, typeof(MyController)))
                     .Returns(expected)
                     .Verifiable();

            var controllerFactory = new DefaultControllerFactory(activator.Object);

            // Act
            var result = controllerFactory.CreateController(actionContext);

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
                ControllerTypeInfo = typeof(ControllerWithActivateAndFromServices).GetTypeInfo()
            };
            var services = GetServices();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services
            };
            var context = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            var factory = new DefaultControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

            // Act
            var result = factory.CreateController(context);

            // Assert
            var controller = Assert.IsType<ControllerWithActivateAndFromServices>(result);
            Assert.Same(context, controller.ActionContext);
            Assert.Same(httpContext, controller.HttpContext);
        }

        [Fact]
        public void CreateController_SetsViewDataDictionary()
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(ControllerWithActivateAndFromServices).GetTypeInfo()
            };

            var services = GetServices();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services
            };
            var context = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            var factory = new DefaultControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

            // Act
            var result = factory.CreateController(context);

            // Assert
            var controller = Assert.IsType<ControllerWithActivateAndFromServices>(result);
            Assert.NotNull(controller.GetViewData());
        }

        [Fact]
        public void CreateController_SetsBindingContext()
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(ControllerWithActivateAndFromServices).GetTypeInfo()
            };
            var bindingContext = new ActionBindingContext();

            var services = GetServices();
            services.GetRequiredService<IScopedInstance<ActionBindingContext>>().Value = bindingContext;
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services
            };
            var context = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            var factory = new DefaultControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

            // Act
            var result = factory.CreateController(context);

            // Assert
            var controller = Assert.IsType<ControllerWithActivateAndFromServices>(result);
            Assert.Same(bindingContext, controller.BindingContext);
        }

        [Fact]
        public void CreateController_PopulatesServicesFromServiceContainer()
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(ControllerWithActivateAndFromServices).GetTypeInfo()
            };
            var services = GetServices();
            var urlHelper = services.GetRequiredService<IUrlHelper>();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = services
            };
            var context = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            var factory = new DefaultControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

            // Act
            var result = factory.CreateController(context);

            // Assert
            var controller = Assert.IsType<ControllerWithActivateAndFromServices>(result);
            Assert.Same(urlHelper, controller.Helper);
        }

        [Fact]
        public void CreateController_PopulatesUserServicesFromServiceContainer()
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(ControllerWithActivateAndFromServices).GetTypeInfo()
            };
            var services = GetServices();
            var testService = services.GetService<TestService>();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = services
            };
            var context = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            var factory = new DefaultControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

            // Act
            var result = factory.CreateController(context);

            // Assert
            var controller = Assert.IsType<ControllerWithActivateAndFromServices>(result);
            Assert.Same(testService, controller.TestService);
        }

        [Fact]
        public void CreateController_IgnoresPropertiesThatAreNotDecoratedWithActivateAttribute()
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(ControllerWithActivateAndFromServices).GetTypeInfo()
            };
            var services = GetServices();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services
            };
            var context = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            var factory = new DefaultControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

            // Act
            var result = factory.CreateController(context);

            // Assert
            var controller = Assert.IsType<ControllerWithActivateAndFromServices>(result);
            Assert.Null(controller.Response);
        }

        [Fact]
        public void CreateController_ThrowsIfPropertyCannotBeActivated()
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(ControllerThatCannotBeActivated).GetTypeInfo()
            };
            var services = GetServices();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services
            };
            var context = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            var factory = new DefaultControllerFactory(new DefaultControllerActivator(new DefaultTypeActivatorCache()));

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateController(context));
            Assert.Equal("The property 'Service' on controller '" + typeof(ControllerThatCannotBeActivated) +
                        "' cannot be activated.", exception.Message);
        }

        [Fact]
        public void DefaultControllerFactory_DisposesIDisposableController()
        {
            // Arrange
            var factory = new DefaultControllerFactory(Mock.Of<IControllerActivator>());
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
            var factory = new DefaultControllerFactory(Mock.Of<IControllerActivator>());
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
            services.Setup(s => s.GetService(typeof(TestService)))
                    .Returns(new TestService());
            services.Setup(s => s.GetService(typeof(IObjectModelValidator)))
                    .Returns(new DefaultObjectValidator(Mock.Of<IValidationExcludeFiltersProvider>(), metadataProvider));
            services
                .Setup(s => s.GetService(typeof(IScopedInstance<ActionBindingContext>)))
                .Returns(new MockScopedInstance<ActionBindingContext>());
            services.Setup(s => s.GetService(typeof(ITempDataDictionary)))
                       .Returns(new Mock<ITempDataDictionary>().Object);
            return services.Object;
        }

        private class ControllerWithActivateAndFromServices
        {
            [Activate]
            public ActionContext ActionContext { get; set; }

            [Activate]
            public ActionBindingContext BindingContext { get; set; }

            [Activate]
            public HttpContext HttpContext { get; set; }

            [Activate]
            protected HttpRequest Request { get; set; }

            [Activate]
            private ViewDataDictionary ViewData { get; set; }

            [FromServices]
            public IUrlHelper Helper { get; set; }

            [FromServices]
            public TestService TestService { get; set; }

            public HttpResponse Response { get; set; }

            public ViewDataDictionary GetViewData()
            {
                return ViewData;
            }

            public HttpRequest GetHttpRequest()
            {
                return Request;
            }
        }

        private class MyController : Controller
        {
            public bool Disposed { get; set; }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
            }
        }

        private class ControllerThatCannotBeActivated
        {
            [Activate]
            public TestService Service { get; set; }
        }

        private class TestService
        {

        }
    }
}
