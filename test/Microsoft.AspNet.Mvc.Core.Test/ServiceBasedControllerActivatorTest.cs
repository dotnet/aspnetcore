// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
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
            var actionContext = new ActionContext(httpContext,
                                                  new RouteData(),
                                                  new ActionDescriptor());

            // Act
            var instance = activator.Create(actionContext, typeof(DIController));

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
            var serviceProvider = new Mock<IServiceProvider>();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider.Object
            };
            var activator = new ServiceBasedControllerActivator();
            var actionContext = new ActionContext(httpContext,
                                                  new RouteData(),
                                                  new ActionDescriptor());

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                        () => activator.Create(actionContext, typeof(DIController)));
            Assert.Equal(expected, ex.Message);
        }

        private class DIController : Controller
        {
        }
    }
}
