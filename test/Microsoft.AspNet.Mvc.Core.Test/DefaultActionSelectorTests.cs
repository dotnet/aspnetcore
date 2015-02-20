// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Http.Core.Collections;
using Microsoft.AspNet.Mvc.ActionConstraints;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Internal;
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

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(DefaultActionSelector).FullName, write.LoggerName);
            Assert.Equal("DefaultActionSelector.SelectAsync", write.Scope);
            var values = Assert.IsType<DefaultActionSelectorSelectAsyncValues>(write.State);
            Assert.Equal("DefaultActionSelector.SelectAsync", values.Name);
            Assert.Empty(values.ActionsMatchingRouteConstraints);
            Assert.Empty(values.ActionsMatchingActionConstraints);
            Assert.Empty(values.FinalMatches);
            Assert.Null(values.SelectedAction);

            // (does not throw)
            Assert.NotEmpty(values.Summary);
        }

        [Fact]
        public async void SelectAsync_MatchedActions_LogIsCorrect()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            var matched = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
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

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(DefaultActionSelector).FullName, write.LoggerName);
            Assert.Equal("DefaultActionSelector.SelectAsync", write.Scope);
            var values = Assert.IsType<DefaultActionSelectorSelectAsyncValues>(write.State);
            Assert.Equal("DefaultActionSelector.SelectAsync", values.Name);
            Assert.Equal<ActionDescriptor>(actions, values.ActionsMatchingRouteConstraints);
            Assert.Equal<ActionDescriptor>(new[] { matched }, values.ActionsMatchingActionConstraints);
            Assert.Equal(matched, Assert.Single(values.FinalMatches));
            Assert.Equal(matched, values.SelectedAction);
        }

        [Fact]
        public async void SelectAsync_AmbiguousActions_LogIsCorrect()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor() { DisplayName = "A1" },
                new ActionDescriptor() { DisplayName = "A2" },
            };

            var selector = CreateSelector(actions, loggerFactory);

            var routeContext = CreateRouteContext("POST");

            // Act
            await Assert.ThrowsAsync<AmbiguousActionException>(async () =>
            {
                await selector.SelectAsync(routeContext);
            });

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(DefaultActionSelector).FullName, scope.LoggerName);
            Assert.Equal("DefaultActionSelector.SelectAsync", scope.Scope);

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(DefaultActionSelector).FullName, write.LoggerName);
            Assert.Equal("DefaultActionSelector.SelectAsync", write.Scope);
            var values = Assert.IsType<DefaultActionSelectorSelectAsyncValues>(write.State);
            Assert.Equal("DefaultActionSelector.SelectAsync", values.Name);
            Assert.Equal<ActionDescriptor>(actions, values.ActionsMatchingRouteConstraints);
            Assert.Equal<ActionDescriptor>(actions, values.ActionsMatchingActionConstraints);
            Assert.Equal<ActionDescriptor>(actions, values.FinalMatches);
            Assert.Null(values.SelectedAction);

            // (does not throw)
            Assert.NotEmpty(values.Summary);
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
                ActionConstraints = new List<IActionConstraintMetadata>()
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
        public async Task SelectAsync_ConstraintsRejectAll()
        {
            // Arrange
            var action1 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = false, },
                },
            };

            var action2 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = false, },
                },
            };

            var actions = new ActionDescriptor[] { action1, action2 };

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("POST");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Null(action);
        }

        [Fact]
        public async Task SelectAsync_ConstraintsRejectAll_DifferentStages()
        {
            // Arrange
            var action1 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = false, Order = 0 },
                    new BooleanConstraint() { Pass = true, Order = 1 },
                },
            };

            var action2 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0 },
                    new BooleanConstraint() { Pass = false, Order = 1 },
                },
            };

            var actions = new ActionDescriptor[] { action1, action2 };

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("POST");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Null(action);
        }

        [Fact]
        public async Task SelectAsync_ActionConstraintFactory()
        {
            // Arrange
            var actionWithConstraints = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new ConstraintFactory()
                    {
                        Constraint = new BooleanConstraint() { Pass = true },
                    },
                }
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
        public async Task SelectAsync_ActionConstraintFactory_ReturnsNull()
        {
            // Arrange
            var nullConstraint = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new ConstraintFactory()
                    {
                    },
                }
            };

            var actions = new ActionDescriptor[] { nullConstraint };

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("POST");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Same(action, nullConstraint);
        }

        // There's a custom constraint provider registered that only understands BooleanConstraintMarker
        [Fact]
        public async Task SelectAsync_CustomProvider()
        {
            // Arrange
            var actionWithConstraints = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraintMarker() { Pass = true },
                }
            };

            var actionWithoutConstraints = new ActionDescriptor()
            {
                Parameters = new List<ParameterDescriptor>(),
            };

            var actions = new ActionDescriptor[] { actionWithConstraints, actionWithoutConstraints, };

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("POST");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Same(action, actionWithConstraints);
        }

        // Due to ordering of stages, the first action will be better.
        [Fact]
        public async Task SelectAsync_ConstraintsInOrder()
        {
            // Arrange
            var best = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                },
            };

            var worst = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 1, },
                },
            };

            var actions = new ActionDescriptor[] { best, worst };

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("POST");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Same(action, best);
        }

        // Due to ordering of stages, the first action will be better.
        [Fact]
        public async Task SelectAsync_ConstraintsInOrder_MultipleStages()
        {
            // Arrange
            var best = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                    new BooleanConstraint() { Pass = true, Order = 1, },
                    new BooleanConstraint() { Pass = true, Order = 2, },
                },
            };

            var worst = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                    new BooleanConstraint() { Pass = true, Order = 1, },
                    new BooleanConstraint() { Pass = true, Order = 3, },
                },
            };

            var actions = new ActionDescriptor[] { best, worst };

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("POST");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Same(action, best);
        }

        [Fact]
        public async Task SelectAsync_Fallback_ToActionWithoutConstraints()
        {
            // Arrange
            var nomatch1 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                    new BooleanConstraint() { Pass = true, Order = 1, },
                    new BooleanConstraint() { Pass = false, Order = 2, },
                },
            };

            var nomatch2 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                    new BooleanConstraint() { Pass = true, Order = 1, },
                    new BooleanConstraint() { Pass = false, Order = 3, },
                },
            };

            var best = new ActionDescriptor();

            var actions = new ActionDescriptor[] { best, nomatch1, nomatch2 };

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("POST");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Same(action, best);
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
            actions[2].RouteConstraints.Add(RouteDataActionConstraint.CreateCatchAll("country"));

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
            actions[2].RouteConstraints.Add(RouteDataActionConstraint.CreateCatchAll("country"));

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
            actions[2].RouteConstraints.Add(RouteDataActionConstraint.CreateCatchAll("country"));

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("GET");

            context.RouteData.Values.Add("controller", "Store");
            context.RouteData.Values.Add("action", "Buy");

            // Act
            var action = await selector.SelectAsync(context);

            // Assert
            Assert.Null(action);
        }

        [Fact]
        public async Task SelectAsync_Ambiguous()
        {
            // Arrange
            var expectedMessage =
                "Multiple actions matched. " +
                "The following actions matched route data and had all constraints satisfied:" + Environment.NewLine +
                Environment.NewLine +
                "Ambiguous1" + Environment.NewLine +
                "Ambiguous2";

            var actions = new ActionDescriptor[]
            {
                CreateAction(area: null, controller: "Store", action: "Buy"),
                CreateAction(area: null, controller: "Store", action: "Buy"),
                CreateAction(area: null, controller: "Store", action: "Cart"),
            };

            actions[0].DisplayName = "Ambiguous1";
            actions[1].DisplayName = "Ambiguous2";

            var selector = CreateSelector(actions);
            var context = CreateRouteContext("GET");

            context.RouteData.Values.Add("controller", "Store");
            context.RouteData.Values.Add("action", "Buy");

            // Act
            var ex = await Assert.ThrowsAsync<AmbiguousActionException>(async () =>
            {
                await selector.SelectAsync(context);
            });

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        [InlineData("POST")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task HttpMethodAttribute_ActionWithMultipleHttpMethodAttributeViaAcceptVerbs_ORsMultipleHttpMethods(string verb)
        {
            // Arrange
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values.Add("controller", "HttpMethodAttributeTests_RestOnly");
            routeContext.RouteData.Values.Add("action", "Patch");

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Equal("Patch", result.Name);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        [InlineData("POST")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task HttpMethodAttribute_ActionWithMultipleHttpMethodAttributes_ORsMultipleHttpMethods(string verb)
        {
            // Arrange
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values.Add("controller", "HttpMethodAttributeTests_RestOnly");
            routeContext.RouteData.Values.Add("action", "Put");

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Equal("Put", result.Name);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        public async Task HttpMethodAttribute_ActionDecoratedWithHttpMethodAttribute_OverridesConvention(string verb)
        {
            // Arrange
            // Note no action name is passed, hence should return a null action descriptor.
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values.Add("controller", "HttpMethodAttributeTests_RestOnly");

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("Put")]
        [InlineData("RPCMethod")]
        [InlineData("RPCMethodWithHttpGet")]
        public void NonActionAttribute_ActionNotReachable(string actionName)
        {
            // Arrange
            var actionDescriptorProvider = GetActionDescriptorProvider();

            // Act
            var result = actionDescriptorProvider.GetDescriptors()
                                                 .Select(x => x as ControllerActionDescriptor)
                                                 .FirstOrDefault(
                                                            x => x.ControllerName == "NonAction" &&
                                                                x.Name == actionName);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        [InlineData("POST")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task ActionNameAttribute_ActionGetsExposedViaActionName_UnreachableByConvention(string verb)
        {
            // Arrange
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values.Add("controller", "ActionName");
            routeContext.RouteData.Values.Add("action", "RPCMethodWithHttpGet");

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("GET", "CustomActionName_Verb")]
        [InlineData("PUT", "CustomActionName_Verb")]
        [InlineData("POST", "CustomActionName_Verb")]
        [InlineData("DELETE", "CustomActionName_Verb")]
        [InlineData("PATCH", "CustomActionName_Verb")]
        [InlineData("GET", "CustomActionName_DefaultMethod")]
        [InlineData("PUT", "CustomActionName_DefaultMethod")]
        [InlineData("POST", "CustomActionName_DefaultMethod")]
        [InlineData("DELETE", "CustomActionName_DefaultMethod")]
        [InlineData("PATCH", "CustomActionName_DefaultMethod")]
        [InlineData("GET", "CustomActionName_RpcMethod")]
        [InlineData("PUT", "CustomActionName_RpcMethod")]
        [InlineData("POST", "CustomActionName_RpcMethod")]
        [InlineData("DELETE", "CustomActionName_RpcMethod")]
        [InlineData("PATCH", "CustomActionName_RpcMethod")]
        public async Task ActionNameAttribute_DifferentActionName_UsesActionNameFromActionNameAttribute(string verb, string actionName)
        {
            // Arrange
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values.Add("controller", "ActionName");
            routeContext.RouteData.Values.Add("action", actionName);

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Equal(actionName, result.Name);
        }

        private async Task<ActionDescriptor> InvokeActionSelector(RouteContext context)
        {
            var actionDescriptorProvider = GetActionDescriptorProvider();

            // service container does not work quite like our built in Depenency Injection container.
            var serviceContainer = new ServiceContainer();
            var list = new List<IActionDescriptorProvider>()
            {
                actionDescriptorProvider,
            };

            serviceContainer.AddService(typeof(IEnumerable<IActionDescriptorProvider>), list);

            var actionCollectionDescriptorProvider = new DefaultActionDescriptorsCollectionProvider(serviceContainer, new NullLoggerFactory());
            var decisionTreeProvider = new ActionSelectorDecisionTreeProvider(actionCollectionDescriptorProvider);

            var actionConstraintProviders = new IActionConstraintProvider[] {
                    new DefaultActionConstraintProvider(),
                };

            var defaultActionSelector = new DefaultActionSelector(
                actionCollectionDescriptorProvider,
                decisionTreeProvider,
                actionConstraintProviders,
                NullLoggerFactory.Instance);

            return await defaultActionSelector.SelectAsync(context);
        }

        private ControllerActionDescriptorProvider GetActionDescriptorProvider()
        {
            var controllerTypes = typeof(DefaultActionSelectorTests)
                .GetNestedTypes(BindingFlags.NonPublic)
                .Select(t => t.GetTypeInfo())
                .ToList();

            var controllerTypeProvider = new FixedSetControllerTypeProvider(controllerTypes);
            var modelBuilder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null),
                                                                 NullLoggerFactory.Instance,
                                                                 null);

            return new ControllerActionDescriptorProvider(
                                        controllerTypeProvider,
                                        modelBuilder,
                                        new TestGlobalFilterProvider(),
                                        new MockMvcOptionsAccessor(),
                                        new NullLoggerFactory());
        }

        private static HttpContext GetHttpContext(string httpMethod)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = httpMethod;
            return httpContext;
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
            var comparer = new RouteValueEqualityComparer();

            return
                actions
                .Where(a => a.RouteConstraints.Any(c => c.RouteKey == "area" && comparer.Equals(c.RouteValue, area)))
                .Where(a => a.RouteConstraints.Any(c => c.RouteKey == "controller" && comparer.Equals(c.RouteValue, controller)))
                .Where(a => a.RouteConstraints.Any(c => c.RouteKey == "action" && comparer.Equals(c.RouteValue, action)));
        }

        private static DefaultActionSelector CreateSelector(IReadOnlyList<ActionDescriptor> actions, ILoggerFactory loggerFactory = null)
        {
            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

            var actionProvider = new Mock<IActionDescriptorsCollectionProvider>(MockBehavior.Strict);

            actionProvider
                .Setup(p => p.ActionDescriptors).Returns(new ActionDescriptorsCollection(actions, 0));

            var decisionTreeProvider = new ActionSelectorDecisionTreeProvider(actionProvider.Object);

            var actionConstraintProviders = new IActionConstraintProvider[] {
                    new DefaultActionConstraintProvider(),
                    new BooleanConstraintProvider(),
                };

            return new DefaultActionSelector(
                actionProvider.Object,
                decisionTreeProvider,
                actionConstraintProviders,
                loggerFactory);
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
            var routeData = new RouteData();
            routeData.Routers.Add(new Mock<IRouter>(MockBehavior.Strict).Object);

            var serviceContainer = new ServiceContainer();

            var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

            var request = new Mock<HttpRequest>(MockBehavior.Strict);
            request.SetupGet(r => r.Method).Returns(httpMethod);
            request.SetupGet(r => r.Path).Returns(new PathString());
            request.SetupGet(r => r.Headers).Returns(new HeaderDictionary());
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.RequestServices).Returns(serviceContainer);

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

        private class BooleanConstraint : IActionConstraint
        {
            public bool Pass { get; set; }

            public int Order { get; set; }

            public bool Accept([NotNull]ActionConstraintContext context)
            {
                return Pass;
            }
        }

        private class ConstraintFactory : IActionConstraintFactory
        {
            public IActionConstraint Constraint { get; set; }

            public IActionConstraint CreateInstance(IServiceProvider services)
            {
                return Constraint;
            }
        }

        private class BooleanConstraintMarker : IActionConstraintMetadata
        {
            public bool Pass { get; set; }
        }

        private class BooleanConstraintProvider : IActionConstraintProvider
        {
            public int Order { get; set; }

            public void OnProvidersExecuting(ActionConstraintProviderContext context)
            {
                foreach (var item in context.Results)
                {
                    var marker = item.Metadata as BooleanConstraintMarker;
                    if (marker != null)
                    {
                        Assert.Null(item.Constraint);
                        item.Constraint = new BooleanConstraint() { Pass = marker.Pass };
                    }
                }
            }

            public void OnProvidersExecuted(ActionConstraintProviderContext context)
            {
            }
        }

        private class NonActionController
        {
            [NonAction]
            public void Put()
            {
            }

            [NonAction]
            public void RPCMethod()
            {
            }

            [NonAction]
            [HttpGet]
            public void RPCMethodWithHttpGet()
            {
            }
        }

        private class HttpMethodAttributeTests_DefaultMethodValidationController
        {
            public void Index()
            {
            }

            // Method with custom attribute.
            [HttpGet]
            public void Get()
            { }

            // InvalidMethod ( since its private)
            private void Post()
            { }
        }

        private class ActionNameController
        {
            [ActionName("CustomActionName_Verb")]
            public void Put()
            {
            }

            [ActionName("CustomActionName_DefaultMethod")]
            public void Index()
            {
            }

            [ActionName("CustomActionName_RpcMethod")]
            public void RPCMethodWithHttpGet()
            {
            }
        }

        private class HttpMethodAttributeTests_RestOnlyController
        {
            [HttpGet]
            [HttpPut]
            [HttpPost]
            [HttpDelete]
            [HttpPatch]
            public void Put()
            {
            }

            [AcceptVerbs("PUT", "post", "GET", "delete", "pATcH")]
            public void Patch()
            {
            }
        }

        private class HttpMethodAttributeTests_DerivedController : HttpMethodAttributeTests_RestOnlyController
        {
        }
    }
}
