// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    public class ServiceBasedControllerActivatorTest
    {
        [Fact]
        public void Create_GetsServicesFromServiceProvider()
        {
            // Arrange
            var controller = new DIController();
            var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            serviceProvider.Setup(s => s.GetService(typeof(DIController)))
                           .Returns(controller)
                           .Verifiable();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider.Object
            };
            var activator = new ServiceBasedControllerActivator();
            var context = new ControllerContext(new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor
                {
                    ControllerTypeInfo = typeof(DIController).GetTypeInfo()
                }));

            // Act
            var instance = activator.Create(context);

            // Assert
            Assert.Same(controller, instance);
            serviceProvider.Verify();
        }

        [Fact]
        public void Create_ThrowsIfControllerIsNotRegisteredInServiceProvider()
        {
            // Arrange
            var expected = "No service for type '" + typeof(DIController) + "' has been registered.";
            var controller = new DIController();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = Mock.Of<IServiceProvider>()
            };

            var activator = new ServiceBasedControllerActivator();
            var context = new ControllerContext(
                new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor
                {
                    ControllerTypeInfo = typeof(DIController).GetTypeInfo()
                }));

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => activator.Create(context));

            Assert.Equal(expected, ex.Message);
        }

        private class Controller
        {
        }

        private class DIController : Controller
        {
        }
    }
}
