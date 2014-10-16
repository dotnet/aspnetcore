// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class DefaultControllerActivatorTest
    {
        [Fact]
        public void Activate_SetsPropertiesFromActionContextHierarchy()
        {
            // Arrange
            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IUrlHelper)))
                    .Returns(Mock.Of<IUrlHelper>());
            services.Setup(s => s.GetService(typeof(IModelMetadataProvider)))
                    .Returns(Mock.Of<IModelMetadataProvider>());

            var httpRequest = Mock.Of<HttpRequest>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request)
                       .Returns(httpRequest);
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(services.Object);
            var routeContext = new RouteContext(httpContext.Object);
            var controller = new TestController();
            var context = new ActionContext(routeContext, new ActionDescriptor())
            {
                Controller = controller
            };
            var activator = new DefaultControllerActivator();

            // Act
            activator.Activate(controller, context);

            // Assert
            Assert.Same(context, controller.ActionContext);
            Assert.Same(httpContext.Object, controller.HttpContext);
            Assert.Same(httpRequest, controller.GetHttpRequest());
        }

        [Fact]
        public void Activate_SetsViewDatDictionary()
        {
            // Arrange
            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IModelMetadataProvider)))
                    .Returns(Mock.Of<IModelMetadataProvider>());
            services.Setup(s => s.GetService(typeof(ICompositeViewEngine)))
                    .Returns(Mock.Of<ICompositeViewEngine>());
            services.Setup(s => s.GetService(typeof(IUrlHelper)))
                    .Returns(Mock.Of<IUrlHelper>());

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(services.Object);
            var routeContext = new RouteContext(httpContext.Object);
            var controller = new TestController();
            var context = new ActionContext(routeContext, new ActionDescriptor())
            {
                Controller = controller
            };
            var activator = new DefaultControllerActivator();

            // Act
            activator.Activate(controller, context);

            // Assert
            Assert.NotNull(controller.GetViewData());
        }

        [Fact]
        public void Activate_PopulatesServicesFromServiceContainer()
        {
            // Arrange
            var urlHelper = Mock.Of<IUrlHelper>();
            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IUrlHelper)))
                    .Returns(urlHelper);
            services.Setup(s => s.GetService(typeof(IModelMetadataProvider)))
                    .Returns(Mock.Of<IModelMetadataProvider>());

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(services.Object);
            var routeContext = new RouteContext(httpContext.Object);
            var controller = new TestController();
            var context = new ActionContext(routeContext, new ActionDescriptor())
            {
                Controller = controller
            };
            var activator = new DefaultControllerActivator();

            // Act
            activator.Activate(controller, context);

            // Assert
            Assert.Same(urlHelper, controller.Helper);
        }

        [Fact]
        public void Activate_IgnoresPropertiesThatAreNotDecoratedWithActivateAttribute()
        {
            // Arrange
            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IUrlHelper)))
                    .Returns(Mock.Of<IUrlHelper>());
            services.Setup(s => s.GetService(typeof(IModelMetadataProvider)))
                    .Returns(Mock.Of<IModelMetadataProvider>());

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Response)
                       .Returns(Mock.Of<HttpResponse>());
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(services.Object);
            var routeContext = new RouteContext(httpContext.Object);
            var controller = new TestController();
            var context = new ActionContext(routeContext, new ActionDescriptor())
            {
                Controller = controller
            };
            var activator = new DefaultControllerActivator();

            // Act
            activator.Activate(controller, context);

            // Assert
            Assert.Null(controller.Response);
        }

        public class TestController
        {
            [Activate]
            public ActionContext ActionContext { get; set; }

            [Activate]
            public HttpContext HttpContext { get; set; }

            [Activate]
            protected HttpRequest Request { get; set; }

            [Activate]
            private ViewDataDictionary ViewData { get; set; }

            [Activate]
            public IUrlHelper Helper { get; set; }

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
    }
}
#endif
