// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public class KnownRouteValueConstraintTests
    {
        private readonly IRouteConstraint _constraint = new KnownRouteValueConstraint();

        [Theory]
        [InlineData("area", RouteDirection.IncomingRequest)]
        [InlineData("controller", RouteDirection.IncomingRequest)]
        [InlineData("action", RouteDirection.IncomingRequest)]
        [InlineData("randomKey", RouteDirection.IncomingRequest)]
        [InlineData("area", RouteDirection.UrlGeneration)]
        [InlineData("controller", RouteDirection.UrlGeneration)]
        [InlineData("action", RouteDirection.UrlGeneration)]
        [InlineData("randomKey", RouteDirection.UrlGeneration)]
        public void RouteKey_DoesNotExist_MatchFails(string keyName, RouteDirection direction)
        {
            // Arrange
            var values = new RouteValueDictionary();
            var httpContext = GetHttpContext(new ActionDescriptor());
            var route = Mock.Of<IRouter>();

            // Act
            var match = _constraint.Match(httpContext, route, keyName, values, direction);

            // Assert
            Assert.False(match);
        }

        [Theory]
        [InlineData("area", RouteDirection.IncomingRequest)]
        [InlineData("controller", RouteDirection.IncomingRequest)]
        [InlineData("action", RouteDirection.IncomingRequest)]
        [InlineData("randomKey", RouteDirection.IncomingRequest)]
        [InlineData("area", RouteDirection.UrlGeneration)]
        [InlineData("controller", RouteDirection.UrlGeneration)]
        [InlineData("action", RouteDirection.UrlGeneration)]
        [InlineData("randomKey", RouteDirection.UrlGeneration)]
        public void RouteKey_Exists_MatchSucceeds(string keyName, RouteDirection direction)
        {
            // Arrange
            var actionDescriptor = CreateActionDescriptor("testArea",
                                                          "testController",
                                                          "testAction");
            actionDescriptor.RouteValues.Add("randomKey", "testRandom");
            var httpContext = GetHttpContext(actionDescriptor);
            var route = Mock.Of<IRouter>();
            var values = new RouteValueDictionary()
            {
                { "area", "testArea" },
                { "controller", "testController" },
                { "action", "testAction" },
                { "randomKey", "testRandom" }
            };

            // Act
            var match = _constraint.Match(httpContext, route, keyName, values, direction);

            // Assert
            Assert.True(match);
        }

        [Theory]
        [InlineData("area", RouteDirection.IncomingRequest)]
        [InlineData("controller", RouteDirection.IncomingRequest)]
        [InlineData("action", RouteDirection.IncomingRequest)]
        [InlineData("randomKey", RouteDirection.IncomingRequest)]
        [InlineData("area", RouteDirection.UrlGeneration)]
        [InlineData("controller", RouteDirection.UrlGeneration)]
        [InlineData("action", RouteDirection.UrlGeneration)]
        [InlineData("randomKey", RouteDirection.UrlGeneration)]
        public void RouteValue_DoesNotExists_MatchFails(string keyName, RouteDirection direction)
        {
            // Arrange
            var actionDescriptor = CreateActionDescriptor(
                "testArea",
                "testController",
                "testAction");
            actionDescriptor.RouteValues.Add("randomKey", "testRandom");
            var httpContext = GetHttpContext(actionDescriptor);
            var route = Mock.Of<IRouter>();
            var values = new RouteValueDictionary()
            {
                { "area", "invalidTestArea" },
                { "controller", "invalidTestController" },
                { "action", "invalidTestAction" },
                { "randomKey", "invalidTestRandom" }
            };

            // Act
            var match = _constraint.Match(httpContext, route, keyName, values, direction);

            // Assert
            Assert.False(match);
        }

        [Theory]
        [InlineData(RouteDirection.IncomingRequest)]
        [InlineData(RouteDirection.UrlGeneration)]
        public void RouteValue_IsNotAString_MatchFails(RouteDirection direction)
        {
            var actionDescriptor = CreateActionDescriptor("testArea",
                                                          controller: null,
                                                          action: null);
            var httpContext = GetHttpContext(actionDescriptor);
            var route = Mock.Of<IRouter>();
            var values = new RouteValueDictionary()
            {
                { "area", 12 },
            };

            // Act
            var match = _constraint.Match(httpContext, route, "area", values, direction);

            // Assert
            Assert.False(match);
        }

        [Theory]
        [InlineData(RouteDirection.IncomingRequest)]
        [InlineData(RouteDirection.UrlGeneration)]
        public void ActionDescriptorCollection_SettingNullValue_Throws(RouteDirection direction)
        {
            // Arrange
            var actionDescriptorCollectionProvider = Mock.Of<IActionDescriptorCollectionProvider>();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .Setup(o => o.RequestServices.GetService(typeof(IActionDescriptorCollectionProvider)))
                .Returns(actionDescriptorCollectionProvider);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => _constraint.Match(
                    httpContext.Object,
                    Mock.Of<IRouter>(),
                    "area",
                    new RouteValueDictionary { { "area", "area" } },
                    direction));
            var providerName = actionDescriptorCollectionProvider.GetType().FullName;
            Assert.Equal(
                $"The 'ActionDescriptors' property of '{providerName}' must not be null.",
                ex.Message);
        }

        private static HttpContext GetHttpContext(ActionDescriptor actionDescriptor)
        {
            var actionProvider = new Mock<IActionDescriptorProvider>(MockBehavior.Strict);

            actionProvider
                .SetupGet(p => p.Order)
                .Returns(-1000);

            actionProvider
                .Setup(p => p.OnProvidersExecuting(It.IsAny<ActionDescriptorProviderContext>()))
                .Callback<ActionDescriptorProviderContext>(c => c.Results.Add(actionDescriptor));

            actionProvider
                .Setup(p => p.OnProvidersExecuted(It.IsAny<ActionDescriptorProviderContext>()))
                .Verifiable();

            var descriptorCollectionProvider = new DefaultActionDescriptorCollectionProvider(
                new[] { actionProvider.Object },
                Enumerable.Empty<IActionDescriptorChangeProvider>());

            var context = new Mock<HttpContext>();
            context.Setup(o => o.RequestServices
                .GetService(typeof(IActionDescriptorCollectionProvider)))
                .Returns(descriptorCollectionProvider);
            return context.Object;
        }

        private static ActionDescriptor CreateActionDescriptor(string area, string controller, string action)
        {
            var actionDescriptor = new ControllerActionDescriptor()
            {
                ActionName = string.Format("Area: {0}, Controller: {1}, Action: {2}", area, controller, action),
            };

            actionDescriptor.RouteValues.Add("area", area);
            actionDescriptor.RouteValues.Add("controller", controller);
            actionDescriptor.RouteValues.Add("action", action);

            return actionDescriptor;
        }
    }
}
