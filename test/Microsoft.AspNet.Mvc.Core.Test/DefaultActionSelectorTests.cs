// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.Framework.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionSelectorTests
    {
        [Fact]
        public async void SelectAsync_NoMatchedActions_LogIsCorrect()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            var routeContext = CreateRouteContext("POST");

            var actions = new ActionDescriptor[0];
            var selector = CreateSelector(actions, loggerFactory);

            // Act
            var action = await selector.SelectAsync(routeContext);

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(DefaultActionSelector).FullName, scope.LoggerName);
            Assert.Equal("DefaultActionSelector.SelectAsync", scope.Scope);

            // There is a record for IsEnabled and one for WriteCore.
            Assert.Equal(2, sink.Writes.Count);

            var enabled = sink.Writes[0];
            Assert.Equal(typeof(DefaultActionSelector).FullName, enabled.LoggerName);
            Assert.Equal("DefaultActionSelector.SelectAsync", enabled.Scope);
            Assert.Null(enabled.State);

            var write = sink.Writes[1];
            Assert.Equal(typeof(DefaultActionSelector).FullName, write.LoggerName);
            Assert.Equal("DefaultActionSelector.SelectAsync", write.Scope);
            var values = Assert.IsType<DefaultActionSelectorSelectAsyncValues>(write.State);
            Assert.Equal("DefaultActionSelector.SelectAsync", values.Name);
            Assert.Empty(values.ActionsMatchingRouteConstraints);
            Assert.Empty(values.ActionsMatchingRouteAndMethodConstraints);
            Assert.Empty(values.ActionsMatchingRouteAndMethodAndDynamicConstraints);
            Assert.Empty(values.ActionsMatchingWithConstraints);
            Assert.Null(values.SelectedAction);
        }

        [Fact]
        public async void SelectAsync_MatchedActions_LogIsCorrect()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            var matched = new ActionDescriptor()
            {
                MethodConstraints = new List<HttpMethodConstraint>()
                {
                    new HttpMethodConstraint(new string[] { "POST" }),
                },
                Parameters = new List<ParameterDescriptor>(),
            };

            var notMatched = new ActionDescriptor()
            {
                Parameters = new List<ParameterDescriptor>(),
            };

            var actions = new ActionDescriptor[] { matched, notMatched };
            var selector = CreateSelector(actions, loggerFactory);

            var routeContext = CreateRouteContext("POST");

            // Act
            var action = await selector.SelectAsync(routeContext);

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(DefaultActionSelector).FullName, scope.LoggerName);
            Assert.Equal("DefaultActionSelector.SelectAsync", scope.Scope);

            // There is a record for IsEnabled and one for WriteCore.
            Assert.Equal(2, sink.Writes.Count);

            var enabled = sink.Writes[0];
            Assert.Equal(typeof(DefaultActionSelector).FullName, enabled.LoggerName);
            Assert.Equal("DefaultActionSelector.SelectAsync", enabled.Scope);
            Assert.Null(enabled.State);

            var write = sink.Writes[1];
            Assert.Equal(typeof(DefaultActionSelector).FullName, write.LoggerName);
            Assert.Equal("DefaultActionSelector.SelectAsync", write.Scope);
            var values = Assert.IsType<DefaultActionSelectorSelectAsyncValues>(write.State);
            Assert.Equal("DefaultActionSelector.SelectAsync", values.Name);
            Assert.NotEmpty(values.ActionsMatchingRouteConstraints);
            Assert.NotEmpty(values.ActionsMatchingRouteAndMethodConstraints);
            Assert.NotEmpty(values.ActionsMatchingWithConstraints);
            Assert.Equal(matched, values.SelectedAction);
        }

        [Fact]
        public void HasValidAction_Match()
        {
            // Arrange
            var actions = GetActions();

            var selector = CreateSelector(actions);
            var context = CreateContext(new { });
            context.ProvidedValues = new RouteValueDictionary(new { controller = "Home", action = "Index" });

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

        [Fact]
        public async Task SelectAsync_WithCatchAll_PrefersNonCatchAll()
        {
            // Arrange
            var actions = new ActionDescriptor[]
            {
                CreateAction(area: null, controller: "Store", action: "Buy"),
                CreateAction(area: null, controller: "Store", action: "Buy"),
                CreateAction(area: null, controller: "Store", action: "Buy"),
            };

            actions[0].RouteConstraints.Add(new RouteDataActionConstraint("country", "CA"));
            actions[1].RouteConstraints.Add(new RouteDataActionConstraint("country", "US"));
            actions[2].RouteConstraints.Add(new RouteDataActionConstraint("country", RouteKeyHandling.CatchAll));

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("GET");

            context.RouteData.Values.Add("controller", "Store");
            context.RouteData.Values.Add("action", "Buy");
            context.RouteData.Values.Add("country", "CA");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Same(action, actions[0]);
        }

        [Fact]
        public async Task SelectAsync_WithCatchAll_CatchAllIsOnlyMatch()
        {
            // Arrange
            var actions = new ActionDescriptor[]
            {
                CreateAction(area: null, controller: "Store", action: "Buy"),
                CreateAction(area: null, controller: "Store", action: "Buy"),
                CreateAction(area: null, controller: "Store", action: "Buy"),
            };

            actions[0].RouteConstraints.Add(new RouteDataActionConstraint("country", "CA"));
            actions[1].RouteConstraints.Add(new RouteDataActionConstraint("country", "US"));
            actions[2].RouteConstraints.Add(new RouteDataActionConstraint("country", RouteKeyHandling.CatchAll));

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("GET");

            context.RouteData.Values.Add("controller", "Store");
            context.RouteData.Values.Add("action", "Buy");
            context.RouteData.Values.Add("country", "DE");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Same(action, actions[2]);
        }

        [Fact]
        public async Task SelectAsync_WithCatchAll_NoMatch()
        {
            // Arrange
            var actions = new ActionDescriptor[]
            {
                CreateAction(area: null, controller: "Store", action: "Buy"),
                CreateAction(area: null, controller: "Store", action: "Buy"),
                CreateAction(area: null, controller: "Store", action: "Buy"),
            };

            actions[0].RouteConstraints.Add(new RouteDataActionConstraint("country", "CA"));
            actions[1].RouteConstraints.Add(new RouteDataActionConstraint("country", "US"));
            actions[2].RouteConstraints.Add(new RouteDataActionConstraint("country", RouteKeyHandling.CatchAll));

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("GET");

            context.RouteData.Values.Add("controller", "Store");
            context.RouteData.Values.Add("action", "Buy");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Null(action);
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

        private static DefaultActionSelector CreateSelector(IReadOnlyList<ActionDescriptor> actions, ILoggerFactory loggerFactory = null)
        {
            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

            var actionProvider = new Mock<IActionDescriptorsCollectionProvider>(MockBehavior.Strict);

            actionProvider
                .Setup(p => p.ActionDescriptors).Returns(new ActionDescriptorsCollection(actions, 0));

            var decisionTreeProvider = new ActionSelectorDecisionTreeProvider(actionProvider.Object);

            var bindingProvider = new Mock<IActionBindingContextProvider>(MockBehavior.Strict);
            bindingProvider
                .Setup(bp => bp.GetActionBindingContextAsync(It.IsAny<ActionContext>()))
                .Returns(Task.FromResult<ActionBindingContext>(null));

            return new DefaultActionSelector(actionProvider.Object, decisionTreeProvider, bindingProvider.Object, loggerFactory);
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
                Parameters = new List<ParameterDescriptor>(),
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
