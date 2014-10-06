// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
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
            var values = new Dictionary<string, object>();
            var httpContext = GetHttpContext(new ActionDescriptor());
            var route = (new Mock<IRouter>()).Object;
            
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
            actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint("randomKey", "testRandom"));
            var httpContext = GetHttpContext(actionDescriptor);
            var route = (new Mock<IRouter>()).Object;
            var values = new Dictionary<string, object>()
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
            var actionDescriptor = CreateActionDescriptor("testArea",
                                                          "testController",
                                                          "testAction");
            actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint("randomKey", "testRandom"));
            var httpContext = GetHttpContext(actionDescriptor);
            var route = (new Mock<IRouter>()).Object;
            var values = new Dictionary<string, object>()
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
            var route = (new Mock<IRouter>()).Object;
            var values = new Dictionary<string, object>()
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
        public void ActionDescriptorsCollection_SettingNullValue_Throws(RouteDirection direction)
        {
            // Arrange
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(o => o.RequestServices
                                  .GetService(typeof(IActionDescriptorsCollectionProvider)))
                       .Returns(new Mock<IActionDescriptorsCollectionProvider>().Object);
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                                    () => _constraint.Match(httpContext.Object,
                                                            null,
                                                            "area",
                                                            new Dictionary<string, object>{ { "area", "area" } },
                                                            direction));
            Assert.Equal("The 'ActionDescriptors' property of "+
                         "'Castle.Proxies.IActionDescriptorsCollectionProviderProxy' must not be null.",
                         ex.Message);
        }

        private static HttpContext GetHttpContext(ActionDescriptor actionDescriptor)
        {
            var actionProvider = new Mock<INestedProviderManager<ActionDescriptorProviderContext>>(
                                                                                    MockBehavior.Strict);

            actionProvider
                .Setup(p => p.Invoke(It.IsAny<ActionDescriptorProviderContext>()))
                .Callback<ActionDescriptorProviderContext>(c => c.Results.Add(actionDescriptor));

            var context = new Mock<HttpContext>();
            context.Setup(o => o.RequestServices
                                .GetService(typeof(INestedProviderManager<ActionDescriptorProviderContext>)))
                   .Returns(actionProvider.Object);
            context.Setup(o => o.RequestServices
                               .GetService(typeof(IActionDescriptorsCollectionProvider)))
                   .Returns(new DefaultActionDescriptorsCollectionProvider(context.Object.RequestServices));
            return context.Object;
        }

        private static ActionDescriptor CreateActionDescriptor(string area, string controller, string action)
        {
            var actionDescriptor = new ActionDescriptor()
            {
                Name = string.Format("Area: {0}, Controller: {1}, Action: {2}", area, controller, action),
                RouteConstraints = new List<RouteDataActionConstraint>(),
            };

            actionDescriptor.RouteConstraints.Add(
                area == null ?
                new RouteDataActionConstraint("area", null) :
                new RouteDataActionConstraint("area", area));

            actionDescriptor.RouteConstraints.Add(
                controller == null ?
                new RouteDataActionConstraint("controller", null) :
                new RouteDataActionConstraint("controller", controller));

            actionDescriptor.RouteConstraints.Add(
                action == null ?
                new RouteDataActionConstraint("action", null) :
                new RouteDataActionConstraint("action", action));

            return actionDescriptor;
        }
    }
}
#endif
