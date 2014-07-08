// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class DefaultActionSelectorTest
    {
        [Fact]
        public void HasValidAction_Match()
        {
            // Arrange
            var actions = GetActions();

            var selector = CreateSelector(actions);
            var context = CreateContext(new { });
            context.ProvidedValues = new RouteValueDictionary(new { controller = "Home", action = "Index"});

            // Act
            var isValid = selector.HasValidAction(context);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void HasValidAction_NoMatch()
        {
            // Arrange
            var actions = GetActions();

            var selector = CreateSelector(actions);
            var context = CreateContext(new { });
            context.ProvidedValues = new RouteValueDictionary(new { controller = "Home", action = "FakeAction" });

            // Act
            var isValid = selector.HasValidAction(context);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task SelectAsync_PrefersActionWithConstraints()
        {
            // Arrange
            var actionWithConstraints = new ActionDescriptor()
            {
                MethodConstraints = new List<HttpMethodConstraint>()
                {
                    new HttpMethodConstraint(new string[] { "POST" }),
                },
                Parameters = new List<ParameterDescriptor>(),
            };

            var actionWithoutConstraints = new ActionDescriptor()
            {
                Parameters = new List<ParameterDescriptor>(),
            };

            var actions = new ActionDescriptor[] { actionWithConstraints, actionWithoutConstraints };

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("POST");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Same(action, actionWithConstraints);
        }

        private static ActionDescriptor[] GetActions()
        {
            return new ActionDescriptor[]
            {
                // Like a typical RPC controller
                CreateAction(area: null, controller: "Home", action: "Index"),
                CreateAction(area: null, controller: "Home", action: "Edit"),

                // Like a typical REST controller
                CreateAction(area: null, controller: "Product", action: null),
                CreateAction(area: null, controller: "Product", action: null),

                // RPC controller in an area with the same name as home
                CreateAction(area: "Admin", controller: "Home", action: "Index"),
                CreateAction(area: "Admin", controller: "Home", action: "Diagnostics"),
            };
        }

        private static IEnumerable<ActionDescriptor> GetActions(
            IEnumerable<ActionDescriptor> actions,
            string area,
            string controller,
            string action)
        {
            return
                actions
                .Where(a => a.RouteConstraints.Any(c => c.RouteKey == "area" && c.Comparer.Equals(c.RouteValue, area)))
                .Where(a => a.RouteConstraints.Any(c => c.RouteKey == "controller" && c.Comparer.Equals(c.RouteValue, controller)))
                .Where(a => a.RouteConstraints.Any(c => c.RouteKey == "action" && c.Comparer.Equals(c.RouteValue, action)));
        }

        private static DefaultActionSelector CreateSelector(IReadOnlyList<ActionDescriptor> actions)
        {
            var actionProvider = new Mock<IActionDescriptorsCollectionProvider>(MockBehavior.Strict);

            actionProvider
                .Setup(p => p.ActionDescriptors).Returns(new ActionDescriptorsCollection(actions, 0));

            var bindingProvider = new Mock<IActionBindingContextProvider>(MockBehavior.Strict);
            bindingProvider
                .Setup(bp => bp.GetActionBindingContextAsync(It.IsAny<ActionContext>()))
                .Returns(() => Task.FromResult<ActionBindingContext>(null));

            return new DefaultActionSelector(actionProvider.Object, bindingProvider.Object);
        }

        private static VirtualPathContext CreateContext(object routeValues)
        {
            return CreateContext(routeValues, ambientValues: null);
        }

        private static VirtualPathContext CreateContext(object routeValues, object ambientValues)
        {
            return new VirtualPathContext(
                new Mock<HttpContext>(MockBehavior.Strict).Object,
                new RouteValueDictionary(ambientValues),
                new RouteValueDictionary(routeValues));
        }

        private static RouteContext CreateRouteContext(string httpMethod)
        {
            var routeData = new RouteData()
            {
                Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
            };

            routeData.Routers.Add(new Mock<IRouter>(MockBehavior.Strict).Object);
            
            var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

            var request = new Mock<HttpRequest>(MockBehavior.Strict);
            httpContext.SetupGet(c => c.Request).Returns(request.Object);

            request.SetupGet(r => r.Method).Returns(httpMethod);
            request.SetupGet(r => r.Path).Returns(new PathString());

            return new RouteContext(httpContext.Object)
            {
                RouteData = routeData,
            };
        }

        private static ActionDescriptor CreateAction(string area, string controller, string action)
        {
            var actionDescriptor = new ActionDescriptor()
            {
                Name = string.Format("Area: {0}, Controller: {1}, Action: {2}", area, controller, action),
                RouteConstraints = new List<RouteDataActionConstraint>(),
            };

            actionDescriptor.RouteConstraints.Add(
                area == null ?
                new RouteDataActionConstraint("area", RouteKeyHandling.DenyKey) :
                new RouteDataActionConstraint("area", area));

            actionDescriptor.RouteConstraints.Add(
                controller == null ?
                new RouteDataActionConstraint("controller", RouteKeyHandling.DenyKey) :
                new RouteDataActionConstraint("controller", controller));

            actionDescriptor.RouteConstraints.Add(
                action == null ?
                new RouteDataActionConstraint("action", RouteKeyHandling.DenyKey) :
                new RouteDataActionConstraint("action", action));

            return actionDescriptor;
        }
    }
}
