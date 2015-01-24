// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Constraints;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Template
{
    public class TemplateRouteTest
    {
        private static IInlineConstraintResolver _inlineConstraintResolver = GetInlineConstraintResolver();

        private async Task<Tuple<TestSink, RouteContext>> SetUp(bool enabled, string template, string requestPath)
        {
            var sink = new TestSink(
                TestSink.EnableWithTypeName<TemplateRoute>,
                TestSink.EnableWithTypeName<TemplateRoute>);
            var loggerFactory = new TestLoggerFactory(sink, enabled);

            var route = CreateRoute(template);
            var context = CreateRouteContext(requestPath, loggerFactory);

            // Act
            await route.RouteAsync(context);

            return Tuple.Create(sink, context);
        }

        [Fact]
        public async Task RouteAsync_MatchSuccess_LogsCorrectValues()
        {
            // Arrange & Act
            var template = "{controller}/{action}";
            var result = await SetUp(true, template, "/Home/Index");
            var sink = result.Item1;
            var context = result.Item2;

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(TemplateRoute).FullName, scope.LoggerName);
            Assert.Equal("TemplateRoute.RouteAsync", scope.Scope);

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(TemplateRoute).FullName, write.LoggerName);
            Assert.Equal("TemplateRoute.RouteAsync", write.Scope);

            // verify WriteCore state contents
            var values = Assert.IsType<TemplateRouteRouteAsyncValues>(write.State);
            Assert.Equal("TemplateRoute.RouteAsync", values.Name);
            Assert.Equal("Home/Index", values.RequestPath);
            Assert.Equal(template, values.Template);
            Assert.NotNull(values.DefaultValues);
            Assert.NotNull(values.ProducedValues);
            Assert.Equal(true, values.MatchedTemplate);
            Assert.Equal(true, values.MatchedConstraints);
            Assert.Equal(true, values.Matched);
            Assert.Equal(context.IsHandled, values.Handled);
        }

        [Fact]
        public async Task RouteAsync_MatchSuccess_DoesNotLogWhenDisabled()
        {
            // Arrange & Act
            var template = "{controller}/{action}";
            var result = await SetUp(false, template, "/Home/Index");
            var sink = result.Item1;

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(TemplateRoute).FullName, scope.LoggerName);
            Assert.Equal("TemplateRoute.RouteAsync", scope.Scope);

            Assert.Equal(0, sink.Writes.Count);
        }

        [Fact]
        public async Task RouteAsync_MatchFailOnValues_LogsCorrectValues()
        {
            // Arrange & Act
            var template = "{controller}/{action}";
            var result = await SetUp(true, template, "/Home/Index/Failure");
            var sink = result.Item1;
            var context = result.Item2;

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(TemplateRoute).FullName, scope.LoggerName);
            Assert.Equal("TemplateRoute.RouteAsync", scope.Scope);

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(TemplateRoute).FullName, write.LoggerName);
            Assert.Equal("TemplateRoute.RouteAsync", write.Scope);
            var values = Assert.IsType<TemplateRouteRouteAsyncValues>(write.State);
            Assert.Equal("TemplateRoute.RouteAsync", values.Name);
            Assert.Equal("Home/Index/Failure", values.RequestPath);
            Assert.Equal(template, values.Template);
            Assert.NotNull(values.DefaultValues);
            Assert.Empty(values.ProducedValues);
            Assert.Equal(false, values.MatchedTemplate);
            Assert.Equal(false, values.MatchedConstraints);
            Assert.Equal(false, values.Matched);
            Assert.Equal(context.IsHandled, values.Handled);
        }

        [Fact]
        public async Task RouteAsync_MatchFailOnValues_DoesNotLogWhenDisabled()
        {
            // Arrange
            var template = "{controller}/{action}";
            var result = await SetUp(false, template, "/Home/Index/Failure");
            var sink = result.Item1;

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(TemplateRoute).FullName, scope.LoggerName);
            Assert.Equal("TemplateRoute.RouteAsync", scope.Scope);

            Assert.Equal(0, sink.Writes.Count);
        }

        [Fact]
        public async Task RouteAsync_MatchFailOnConstraints_LogsCorrectValues()
        {
            // Arrange & Act
            var template = "{controller}/{action}/{id:int}";
            var result = await SetUp(true, template, "/Home/Index/Failure");
            var sink = result.Item1;
            var context = result.Item2;

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(TemplateRoute).FullName, scope.LoggerName);
            Assert.Equal("TemplateRoute.RouteAsync", scope.Scope);

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(TemplateRoute).FullName, write.LoggerName);
            Assert.Equal("TemplateRoute.RouteAsync", write.Scope);
            var values = Assert.IsType<TemplateRouteRouteAsyncValues>(write.State);
            Assert.Equal("TemplateRoute.RouteAsync", values.Name);
            Assert.Equal("Home/Index/Failure", values.RequestPath);
            Assert.Equal(template, values.Template);
            Assert.NotNull(values.DefaultValues);
            Assert.NotNull(values.ProducedValues);
            Assert.Equal(true, values.MatchedTemplate);
            Assert.Equal(false, values.MatchedConstraints);
            Assert.Equal(false, values.Matched);
            Assert.Equal(context.IsHandled, values.Handled);
        }

        [Fact]
        public async Task RouteAsync_MergesExistingRouteData_IfRouteMatches()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int}";

            var context = CreateRouteContext("/Home/Index/5");

            var originalRouteDataValues = context.RouteData.Values;
            originalRouteDataValues.Add("country", "USA");

            var originalDataTokens = context.RouteData.DataTokens;
            originalDataTokens.Add("company", "Contoso");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.IsHandled = true;
                })
                .Returns(Task.FromResult(true));

            var route = new TemplateRoute(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: new RouteValueDictionary(new { today = "Friday" }),
                inlineConstraintResolver: _inlineConstraintResolver);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(routeValues);

            Assert.True(routeValues.ContainsKey("country"));
            Assert.Equal("USA", routeValues["country"]);
            Assert.True(routeValues.ContainsKey("id"));
            Assert.Equal("5", routeValues["id"]);

            Assert.True(context.RouteData.Values.ContainsKey("country"));
            Assert.Equal("USA", context.RouteData.Values["country"]);
            Assert.True(context.RouteData.Values.ContainsKey("id"));
            Assert.Equal("5", context.RouteData.Values["id"]);
            Assert.NotSame(originalRouteDataValues, context.RouteData.Values);

            Assert.Equal("Contoso", context.RouteData.DataTokens["company"]);
            Assert.Equal("Friday", context.RouteData.DataTokens["today"]);
            Assert.NotSame(originalDataTokens, context.RouteData.DataTokens);
            Assert.NotSame(route.DataTokens, context.RouteData.DataTokens);
        }

        [Fact]
        public async Task RouteAsync_MergesExistingRouteData_PassedToConstraint()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int}";

            var context = CreateRouteContext("/Home/Index/5");
            var originalRouteDataValues = context.RouteData.Values;
            originalRouteDataValues.Add("country", "USA");

            var originalDataTokens = context.RouteData.DataTokens;
            originalDataTokens.Add("company", "Contoso");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.IsHandled = true;
                })
                .Returns(Task.FromResult(true));

            var constraint = new CapturingConstraint();

            var route = new TemplateRoute(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: new RouteValueDictionary(new { action = constraint }),
                dataTokens: new RouteValueDictionary(new { today = "Friday" }),
                inlineConstraintResolver: _inlineConstraintResolver);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(routeValues);

            Assert.True(routeValues.ContainsKey("country"));
            Assert.Equal("USA", routeValues["country"]);
            Assert.True(routeValues.ContainsKey("id"));
            Assert.Equal("5", routeValues["id"]);

            Assert.True(constraint.Values.ContainsKey("country"));
            Assert.Equal("USA", constraint.Values["country"]);
            Assert.True(constraint.Values.ContainsKey("id"));
            Assert.Equal("5", constraint.Values["id"]);

            Assert.True(context.RouteData.Values.ContainsKey("country"));
            Assert.Equal("USA", context.RouteData.Values["country"]);
            Assert.True(context.RouteData.Values.ContainsKey("id"));
            Assert.Equal("5", context.RouteData.Values["id"]);
            Assert.NotSame(originalRouteDataValues, context.RouteData.Values);

            Assert.Equal("Contoso", context.RouteData.DataTokens["company"]);
            Assert.Equal("Friday", context.RouteData.DataTokens["today"]);
            Assert.NotSame(originalDataTokens, context.RouteData.DataTokens);
            Assert.NotSame(route.DataTokens, context.RouteData.DataTokens);
        }

        [Fact]
        public async Task RouteAsync_CleansUpMergedRouteData_IfRouteDoesNotMatch()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int}";

            var context = CreateRouteContext("/Home/Index/5");
            var originalRouteDataValues = context.RouteData.Values;
            originalRouteDataValues.Add("country", "USA");

            var originalDataTokens = context.RouteData.DataTokens;
            originalDataTokens.Add("company", "Contoso");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.IsHandled = false;
                })
                .Returns(Task.FromResult(true));

            var route = new TemplateRoute(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: new RouteValueDictionary(new { today = "Friday" }),
                inlineConstraintResolver: _inlineConstraintResolver);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.NotNull(routeValues);

            Assert.True(routeValues.ContainsKey("country"));
            Assert.Equal("USA", routeValues["country"]);
            Assert.True(routeValues.ContainsKey("id"));
            Assert.Equal("5", routeValues["id"]);

            Assert.True(context.RouteData.Values.ContainsKey("country"));
            Assert.Equal("USA", context.RouteData.Values["country"]);
            Assert.False(context.RouteData.Values.ContainsKey("id"));
            Assert.Same(originalRouteDataValues, context.RouteData.Values);

            Assert.Equal("Contoso", context.RouteData.DataTokens["company"]);
            Assert.False(context.RouteData.DataTokens.ContainsKey("today"));
            Assert.Same(originalDataTokens, context.RouteData.DataTokens);
        }

        [Fact]
        public async Task RouteAsync_CleansUpMergedRouteData_IfHandlerThrows()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int}";

            var context = CreateRouteContext("/Home/Index/5");
            var originalRouteDataValues = context.RouteData.Values;
            originalRouteDataValues.Add("country", "USA");

            var originalDataTokens = context.RouteData.DataTokens;
            originalDataTokens.Add("company", "Contoso");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.IsHandled = false;
                })
                .Throws(new Exception());

            var route = new TemplateRoute(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: new RouteValueDictionary(new { today = "Friday" }),
                inlineConstraintResolver: _inlineConstraintResolver);

            // Act
            var ex = await Assert.ThrowsAsync<Exception>(() => route.RouteAsync(context));

            // Assert
            Assert.NotNull(routeValues);

            Assert.True(routeValues.ContainsKey("country"));
            Assert.Equal("USA", routeValues["country"]);
            Assert.True(routeValues.ContainsKey("id"));
            Assert.Equal("5", routeValues["id"]);

            Assert.True(context.RouteData.Values.ContainsKey("country"));
            Assert.Equal("USA", context.RouteData.Values["country"]);
            Assert.False(context.RouteData.Values.ContainsKey("id"));
            Assert.Same(originalRouteDataValues, context.RouteData.Values);

            Assert.Equal("Contoso", context.RouteData.DataTokens["company"]);
            Assert.False(context.RouteData.DataTokens.ContainsKey("today"));
            Assert.Same(originalDataTokens, context.RouteData.DataTokens);
        }

        [Fact]
        public async Task RouteAsync_CleansUpMergedRouteData_IfConstraintThrows()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int}";

            var context = CreateRouteContext("/Home/Index/5");
            var originalRouteDataValues = context.RouteData.Values;
            originalRouteDataValues.Add("country", "USA");

            var originalDataTokens = context.RouteData.DataTokens;
            originalDataTokens.Add("company", "Contoso");

            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    ctx.IsHandled = true;
                })
                .Returns(Task.FromResult(true));

            var constraint = new Mock<IRouteConstraint>(MockBehavior.Strict);
            constraint
                .Setup(c => c.Match(
                    It.IsAny<HttpContext>(),
                    It.IsAny<IRouter>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, object>>(),
                    It.IsAny<RouteDirection>()))
                .Callback(() => { throw new Exception(); });

            var route = new TemplateRoute(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: new RouteValueDictionary(new { action = constraint.Object }),
                dataTokens: new RouteValueDictionary(new { today = "Friday" }),
                inlineConstraintResolver: _inlineConstraintResolver);

            // Act
            var ex = await Assert.ThrowsAsync<Exception>(() => route.RouteAsync(context));

            // Assert
            Assert.True(context.RouteData.Values.ContainsKey("country"));
            Assert.Equal("USA", context.RouteData.Values["country"]);
            Assert.False(context.RouteData.Values.ContainsKey("id"));
            Assert.Same(originalRouteDataValues, context.RouteData.Values);

            Assert.Equal("Contoso", context.RouteData.DataTokens["company"]);
            Assert.False(context.RouteData.DataTokens.ContainsKey("today"));
            Assert.Same(originalDataTokens, context.RouteData.DataTokens);
        }

        [Fact]
        public async Task RouteAsync_MatchFailOnConstraints_DoesNotLogWhenDisabled()
        {
            // Arrange & Act
            var template = "{controller}/{action}/{id:int}";
            var result = await SetUp(false, template, "/Home/Index/Failure");
            var sink = result.Item1;

            // Assert
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(TemplateRoute).FullName, scope.LoggerName);
            Assert.Equal("TemplateRoute.RouteAsync", scope.Scope);

            Assert.Equal(0, sink.Writes.Count);
        }

        [Fact]
        public async Task RouteAsync_InlineConstraint_OptionalParameter()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int?}";

            var context = CreateRouteContext("/Home/Index/5");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.IsHandled = true;
                })
                .Returns(Task.FromResult(true));

            var route = new TemplateRoute(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);

            Assert.NotEmpty(route.Constraints);
            Assert.IsType<OptionalRouteConstraint>(route.Constraints["id"]);

            // Act
            await route.RouteAsync(context);

            // Assert            
            Assert.True(context.IsHandled);
            Assert.True(routeValues.ContainsKey("id"));
            Assert.Equal("5", routeValues["id"]);

            Assert.True(context.RouteData.Values.ContainsKey("id"));
            Assert.Equal("5", context.RouteData.Values["id"]);
        }

        [Fact]
        public async Task RouteAsync_InlineConstraint_OptionalParameter_NotPresent()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int?}";

            var context = CreateRouteContext("/Home/Index");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.IsHandled = true;
                })
                .Returns(Task.FromResult(true));

            var route = new TemplateRoute(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);

            Assert.NotEmpty(route.Constraints);
            Assert.IsType<OptionalRouteConstraint>(route.Constraints["id"]);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.True(context.IsHandled);
            Assert.NotNull(routeValues);
            Assert.False(routeValues.ContainsKey("id"));
            Assert.False(context.RouteData.Values.ContainsKey("id"));
        }

        [Fact]
        public async Task RouteAsync_InlineConstraint_OptionalParameter_WithInConstructorConstraint()
        {
            // Arrange
            var template = "{controller}/{action}/{id:int?}";

            var context = CreateRouteContext("/Home/Index/5");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.IsHandled = true;
                })
                .Returns(Task.FromResult(true));

            var constraints = new Dictionary<string, object>();
            constraints.Add("id", new RangeRouteConstraint(1, 20));

            var route = new TemplateRoute(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: constraints,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);

            Assert.NotEmpty(route.Constraints);
            Assert.IsType<OptionalRouteConstraint>(route.Constraints["id"]);
            var innerConstraint = ((OptionalRouteConstraint)route.Constraints["id"]).InnerConstraint;
            Assert.IsType<CompositeRouteConstraint>(innerConstraint);
            var compositeConstraint = (CompositeRouteConstraint)innerConstraint;
            Assert.Equal(compositeConstraint.Constraints.Count<IRouteConstraint>(), 2);

            Assert.Single(compositeConstraint.Constraints, c => c is IntRouteConstraint);
            Assert.Single(compositeConstraint.Constraints, c => c is RangeRouteConstraint);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.True(context.IsHandled);
            Assert.True(routeValues.ContainsKey("id"));
            Assert.Equal("5", routeValues["id"]);

            Assert.True(context.RouteData.Values.ContainsKey("id"));
            Assert.Equal("5", context.RouteData.Values["id"]);
        }

        [Fact]
        public async Task RouteAsync_InlineConstraint_OptionalParameter_ConstraintFails()
        {
            // Arrange
            var template = "{controller}/{action}/{id:range(1,20)?}";

            var context = CreateRouteContext("/Home/Index/100");

            IDictionary<string, object> routeValues = null;
            var mockTarget = new Mock<IRouter>(MockBehavior.Strict);
            mockTarget
                .Setup(s => s.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(ctx =>
                {
                    routeValues = ctx.RouteData.Values;
                    ctx.IsHandled = true;
                })
                .Returns(Task.FromResult(true));

            var route = new TemplateRoute(
                mockTarget.Object,
                template,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: _inlineConstraintResolver);

            Assert.NotEmpty(route.Constraints);
            Assert.IsType<OptionalRouteConstraint>(route.Constraints["id"]);

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.False(context.IsHandled);
        }

        #region Route Matching

        // PathString in HttpAbstractions guarantees a leading slash - so no value in testing other cases.
        [Fact]
        public async Task Match_Success_LeadingSlash()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateRouteContext("/Home/Index");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.True(context.IsHandled);
            Assert.Equal(2, context.RouteData.Values.Count);
            Assert.Equal("Home", context.RouteData.Values["controller"]);
            Assert.Equal("Index", context.RouteData.Values["action"]);
        }

        [Fact]
        public async Task Match_Success_RootUrl()
        {
            // Arrange
            var route = CreateRoute("");
            var context = CreateRouteContext("/");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.True(context.IsHandled);
            Assert.Equal(0, context.RouteData.Values.Count);
        }

        [Fact]
        public async Task Match_Success_Defaults()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}", new { action = "Index" });
            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.True(context.IsHandled);
            Assert.Equal(2, context.RouteData.Values.Count);
            Assert.Equal("Home", context.RouteData.Values["controller"]);
            Assert.Equal("Index", context.RouteData.Values["action"]);
        }

        [Fact]
        public async Task Match_Success_CopiesDataTokens()
        {
            // Arrange
            var route = CreateRoute(
                "{controller}/{action}",
                defaults: new { action = "Index" },
                dataTokens: new { culture = "en-CA" });

            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);
            Assert.True(context.IsHandled);

            // This should not affect the route - RouteData.DataTokens is a copy
            context.RouteData.DataTokens.Add("company", "contoso");

            // Assert
            Assert.Single(route.DataTokens);
            Assert.Single(route.DataTokens, kvp => kvp.Key == "culture" && ((string)kvp.Value) == "en-CA");
        }

        [Fact]
        public async Task Match_Fails()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.False(context.IsHandled);
        }

        [Fact]
        public async Task Match_RejectedByHandler()
        {
            // Arrange
            var route = CreateRoute("{controller}", accept: false);
            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.False(context.IsHandled);

            // Issue #16 tracks this.
            Assert.Empty(context.RouteData.Values);
        }

        [Fact]
        public async Task Match_RejectedByHandler_ClearsRouters()
        {
            // Arrange
            var route = CreateRoute("{controller}", accept: false);
            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.False(context.IsHandled);
            Assert.Empty(context.RouteData.Routers);
        }

        [Fact]
        public async Task Match_SetsRouters()
        {
            // Arrange
            var target = CreateTarget(accept: true);
            var route = CreateRoute(target, "{controller}");
            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.True(context.IsHandled);
            Assert.Equal(1, context.RouteData.Routers.Count);
            Assert.Same(target, context.RouteData.Routers[0]);
        }

        [Fact]
        public async Task Match_RouteValuesDoesntThrowOnKeyNotFound()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateRouteContext("/Home/Index");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Null(context.RouteData.Values["1controller"]);
        }

        [Fact]
        public async Task Match_Success_OptionalParameter_ValueProvided()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}.{format?}", new { action = "Index" });
            var context = CreateRouteContext("/Home/Create.xml");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.True(context.IsHandled);
            Assert.Equal(3, context.RouteData.Values.Count);
            Assert.Equal("Home", context.RouteData.Values["controller"]);
            Assert.Equal("Create", context.RouteData.Values["action"]);
            Assert.Equal("xml", context.RouteData.Values["format"]);
        }

        [Fact]
        public async Task Match_Success_OptionalParameter_ValueNotProvided()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}.{format?}", new { action = "Index" });
            var context = CreateRouteContext("/Home/Create");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.True(context.IsHandled);
            Assert.Equal(2, context.RouteData.Values.Count);
            Assert.Equal("Home", context.RouteData.Values["controller"]);
            Assert.Equal("Create", context.RouteData.Values["action"]);
        }

        [Fact]
        public async Task Match_Success_OptionalParameter_DefaultValue()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}.{format?}", new { action = "Index", format = "xml" });
            var context = CreateRouteContext("/Home/Create");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.True(context.IsHandled);
            Assert.Equal(3, context.RouteData.Values.Count);
            Assert.Equal("Home", context.RouteData.Values["controller"]);
            Assert.Equal("Create", context.RouteData.Values["action"]);
            Assert.Equal("xml", context.RouteData.Values["format"]);
        }

        [Fact]
        public async Task Match_Success_OptionalParameter_EndsWithDot()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}.{format?}", new { action = "Index" });
            var context = CreateRouteContext("/Home/Create.");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.False(context.IsHandled);
        }

        private static RouteContext CreateRouteContext(string requestPath, ILoggerFactory factory = null)
        {
            if (factory == null)
            {
                factory = NullLoggerFactory.Instance;
            }

            var request = new Mock<HttpRequest>(MockBehavior.Strict);
            request.SetupGet(r => r.Path).Returns(new PathString(requestPath));

            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(factory);
            context.SetupGet(c => c.Request).Returns(request.Object);

            return new RouteContext(context.Object);
        }
        #endregion

        #region Route Binding

        [Fact]
        public void GetVirtualPath_Success()
        {
            // Arrange
            var route = CreateRoute("{controller}");
            var context = CreateVirtualPathContext(new { controller = "Home" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.True(context.IsBound);
            Assert.Equal("Home", path);
        }

        [Fact]
        public void GetVirtualPath_Fail()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateVirtualPathContext(new { controller = "Home" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.False(context.IsBound);
            Assert.Null(path);
        }

        [Fact]
        public void GetVirtualPath_RejectedByHandler()
        {
            // Arrange
            var route = CreateRoute("{controller}", accept: false);
            var context = CreateVirtualPathContext(new { controller = "Home" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.False(context.IsBound);
            Assert.Null(path);
        }

        [Fact]
        public void GetVirtualPath_Success_AmbientValues()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateVirtualPathContext(new { action = "Index" }, new { controller = "Home" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.True(context.IsBound);
            Assert.Equal("Home/Index", path);
        }

        [Fact]
        public void RouteGenerationRejectsConstraints()
        {
            // Arrange
            var context = CreateVirtualPathContext(new { p1 = "abcd" });

            TemplateRoute r = CreateRoute(
                "{p1}/{p2}",
                new { p2 = "catchall" },
                true,
                new RouteValueDictionary(new { p2 = "\\d{4}" }));

            // Act
            var virtualPath = r.GetVirtualPath(context);

            // Assert
            Assert.False(context.IsBound);
            Assert.Null(virtualPath);
        }

        [Fact]
        public void RouteGenerationAcceptsConstraints()
        {
            // Arrange
            var context = CreateVirtualPathContext(new { p1 = "hello", p2 = "1234" });

            TemplateRoute r = CreateRoute(
                "{p1}/{p2}",
                new { p2 = "catchall" },
                true,
                new RouteValueDictionary(new { p2 = "\\d{4}" }));

            // Act
            var virtualPath = r.GetVirtualPath(context);

            // Assert
            Assert.True(context.IsBound);
            Assert.NotNull(virtualPath);
            Assert.Equal("hello/1234", virtualPath);
        }

        [Fact]
        public void RouteWithCatchAllRejectsConstraints()
        {
            // Arrange
            var context = CreateVirtualPathContext(new { p1 = "abcd" });

            TemplateRoute r = CreateRoute(
                "{p1}/{*p2}",
                new { p2 = "catchall" },
                true,
                new RouteValueDictionary(new { p2 = "\\d{4}" }));

            // Act
            var virtualPath = r.GetVirtualPath(context);

            // Assert
            Assert.False(context.IsBound);
            Assert.Null(virtualPath);
        }

        [Fact]
        public void RouteWithCatchAllAcceptsConstraints()
        {
            // Arrange
            // Arrange
            var context = CreateVirtualPathContext(new { p1 = "hello", p2 = "1234" });

            TemplateRoute r = CreateRoute(
                "{p1}/{*p2}",
                new { p2 = "catchall" },
                true,
                new RouteValueDictionary(new { p2 = "\\d{4}" }));

            // Act
            var virtualPath = r.GetVirtualPath(context);

            // Assert
            Assert.True(context.IsBound);
            Assert.NotNull(virtualPath);
            Assert.Equal("hello/1234", virtualPath);
        }

        [Fact]
        public void GetVirtualPathWithNonParameterConstraintReturnsUrlWithoutQueryString()
        {
            // Arrange
            var context = CreateVirtualPathContext(new { p1 = "hello", p2 = "1234" });

            var target = new Mock<IRouteConstraint>();
            target.Setup(e => e.Match(It.IsAny<HttpContext>(),
                                      It.IsAny<IRouter>(),
                                      It.IsAny<string>(),
                                      It.IsAny<IDictionary<string, object>>(),
                                      It.IsAny<RouteDirection>()))
                .Returns(true)
                .Verifiable();

            TemplateRoute r = CreateRoute(
                "{p1}/{p2}",
                new { p2 = "catchall" },
                true,
                new RouteValueDictionary(new { p2 = target.Object }));

            // Act
            var virtualPath = r.GetVirtualPath(context);

            // Assert
            Assert.True(context.IsBound);
            Assert.NotNull(virtualPath);
            Assert.Equal("hello/1234", virtualPath);

            target.VerifyAll();
        }

        [Fact]
        public void GetVirtualPath_Sends_ProvidedValues()
        {
            // Arrange
            VirtualPathContext childContext = null;
            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(c => { childContext = c; c.IsBound = true; })
                .Returns<string>(null);

            var route = CreateRoute(target.Object, "{controller}/{action}");
            var context = CreateVirtualPathContext(
                new { action = "Store" },
                new { Controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(new { controller = "Home", action = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Store", path);
            Assert.Equal(expectedValues, childContext.ProvidedValues);
        }

        [Fact]
        public void GetVirtualPath_Sends_ProvidedValues_IncludingDefaults()
        {
            // Arrange
            VirtualPathContext childContext = null;
            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(c => { childContext = c; c.IsBound = true; })
                .Returns<string>(null);

            var route = CreateRoute(target.Object, "Admin/{controller}/{action}", new { area = "Admin" });
            var context = CreateVirtualPathContext(
                new { action = "Store" }, new { Controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store", area = "Admin" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Admin/Home/Store", path);
            Assert.Equal(expectedValues, childContext.ProvidedValues);
        }

        [Fact]
        public void GetVirtualPath_Sends_ProvidedValues_ButNotQueryStringValues()
        {
            // Arrange
            VirtualPathContext childContext = null;
            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(c => { childContext = c; c.IsBound = true; })
                .Returns<string>(null);

            var route = CreateRoute(target.Object, "{controller}/{action}");
            var context = CreateVirtualPathContext(
                new { action = "Store", id = 5 }, new { Controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(new { controller = "Home", action = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Store?id=5", path);
            Assert.Equal(expectedValues, childContext.ProvidedValues);
        }

        // Any ambient values from the current request should be visible to constraint, even
        // if they have nothing to do with the route generating a link
        [Fact]
        public void GetVirtualPath_ConstraintsSeeAmbientValues()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var route = CreateRoute(
                template: "slug/{controller}/{action}",
                defaults: null,
                accept: true,
                constraints: new { c = constraint });

            var context = CreateVirtualPathContext(
                values: new { action = "Store" },
                ambientValues: new { Controller = "Home", action = "Blog", extra = "42" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store", extra = "42" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("slug/Home/Store", path);
            Assert.Equal(expectedValues, constraint.Values);
        }

        // Non-parameter default values from the routing generating a link are not in the 'values'
        // collection when constraints are processed.
        [Fact]
        public void GetVirtualPath_ConstraintsDontSeeDefaults_WhenTheyArentParameters()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var route = CreateRoute(
                template: "slug/{controller}/{action}",
                defaults: new { otherthing = "17" },
                accept: true,
                constraints: new { c = constraint });

            var context = CreateVirtualPathContext(
                values: new { action = "Store" },
                ambientValues: new { Controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("slug/Home/Store", path);
            Assert.Equal(expectedValues, constraint.Values);
        }

        // Default values are visible to the constraint when they are used to fill a parameter.
        [Fact]
        public void GetVirtualPath_ConstraintsSeesDefault_WhenThereItsAParamter()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var route = CreateRoute(
                template: "slug/{controller}/{action}",
                defaults: new { action = "Index" },
                accept: true,
                constraints: new { c = constraint });

            var context = CreateVirtualPathContext(
                values: new { controller = "Shopping" },
                ambientValues: new { Controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Shopping", action = "Index" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("slug/Shopping", path);
            Assert.Equal(expectedValues, constraint.Values);
        }

        // Default values from the routing generating a link are in the 'values' collection when
        // constraints are processed - IFF they are specified as values or ambient values.
        [Fact]
        public void GetVirtualPath_ConstraintsSeeDefaults_IfTheyAreSpecifiedOrAmbient()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var route = CreateRoute(
                template: "slug/{controller}/{action}",
                defaults: new { otherthing = "17", thirdthing = "13" },
                accept: true,
                constraints: new { c = constraint });

            var context = CreateVirtualPathContext(
                values: new { action = "Store", thirdthing = "13" },
                ambientValues: new { Controller = "Home", action = "Blog", otherthing = "17" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store", otherthing = "17", thirdthing = "13" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("slug/Home/Store", path);
            Assert.Equal(expectedValues.OrderBy(kvp => kvp.Key), constraint.Values.OrderBy(kvp => kvp.Key));
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_Success()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", id = 4 });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/4", path);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_NonMatchingvalue()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", id = "asf" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_OptionalParameter_ValuePresent()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int?}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", id = 98 });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/98", path);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_OptionalParameter_ValueNotPresent()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int?}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index", path);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_OptionalParameter_ValuePresent_ConstraintFails()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int?}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", id = "sdfd" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_CompositeInlineConstraint()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}/{id:int:range(1,20)}");
            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", id = 14 });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/14", path);
        }

        [Fact]
        public void GetVirtualPath_InlineConstraints_CompositeConstraint_FromConstructor()
        {
            // Arrange
            var constraint = new MaxLengthRouteConstraint(20);
            var route = CreateRoute(
                template: "{controller}/{action}/{name:alpha}",
                defaults: null,
                accept: true,
                constraints: new { name = constraint });

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/products", path);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_ParameterPresentInValues()
        {
            // Arrange            
            var route = CreateRoute(
                template: "{controller}/{action}/{name}.{format?}",
                defaults: null,
                accept: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products", format = "xml" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/products.xml", path);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_ParameterNotPresentInValues()
        {
            // Arrange            
            var route = CreateRoute(
                template: "{controller}/{action}/{name}.{format?}",
                defaults: null,
                accept: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/products", path);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_ParameterPresentInValuesAndDefaults()
        {
            // Arrange            
            var route = CreateRoute(
                template: "{controller}/{action}/{name}.{format?}",
                defaults: new { format = "json" },
                accept: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products", format = "xml" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/products.xml", path);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_ParameterNotPresentInValues_PresentInDefaults()
        {
            // Arrange            
            var route = CreateRoute(
                template: "{controller}/{action}/{name}.{format?}",
                defaults: new { format = "json" },
                accept: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/products", path);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_ParameterNotPresentInTemplate_PresentInValues()
        {
            // Arrange            
            var route = CreateRoute(
                template: "{controller}/{action}/{name}",
                defaults: null,
                accept: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products", format = "json" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/products?format=json", path);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_FollowedByDotAfterSlash_ParameterPresent()
        {
            // Arrange            
            var route = CreateRoute(
                template: "{controller}/{action}/.{name?}",
                defaults: null,
                accept: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/.products", path);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_FollowedByDotAfterSlash_ParameterNotPresent()
        {
            // Arrange            
            var route = CreateRoute(
                template: "{controller}/{action}/.{name?}",
                defaults: null,
                accept: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index/", path);
        }

        [Fact]
        public void GetVirtualPath_OptionalParameter_InSimpleSegment()
        {
            // Arrange            
            var route = CreateRoute(
                template: "{controller}/{action}/{name?}",
                defaults: null,
                accept: true,
                constraints: null);

            var context = CreateVirtualPathContext(
                values: new { action = "Index", controller = "Home" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Index", path);
        }

        private static VirtualPathContext CreateVirtualPathContext(object values)
        {
            return CreateVirtualPathContext(new RouteValueDictionary(values), null);
        }

        private static VirtualPathContext CreateVirtualPathContext(object values, object ambientValues)
        {
            return CreateVirtualPathContext(new RouteValueDictionary(values), new RouteValueDictionary(ambientValues));
        }

        private static VirtualPathContext CreateVirtualPathContext(
            IDictionary<string, object> values,
            IDictionary<string, object> ambientValues)
        {
            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(NullLoggerFactory.Instance);

            return new VirtualPathContext(context.Object, ambientValues, values);
        }

        private static VirtualPathContext CreateVirtualPathContext(string routeName)
        {
            return new VirtualPathContext(null, null, null, routeName);
        }

        #endregion

        #region Route Registration

        public static IEnumerable<object[]> DataTokens
        {
            get
            {
                yield return new object[] {
                                            new Dictionary<string, object> { { "key1", "data1" }, { "key2", 13 } },
                                            new Dictionary<string, object> { { "key1", "data1" }, { "key2", 13 } },
                                          };
                yield return new object[] {
                                            new RouteValueDictionary { { "key1", "data1" }, { "key2", 13 } },
                                            new Dictionary<string, object> { { "key1", "data1" }, { "key2", 13 } },
                                          };
                yield return new object[] {
                                            new object(),
                                            new Dictionary<string,object>(),
                                          };
                yield return new object[] {
                                            null,
                                            new Dictionary<string, object>()
                                          };
                yield return new object[] {
                                            new { key1 = "data1", key2 = 13 },
                                            new Dictionary<string, object> { { "key1", "data1" }, { "key2", 13 } },
                                          };
            }
        }

        [Theory]
        [MemberData("DataTokens")]
        public void RegisteringRoute_WithDataTokens_AbleToAddTheRoute(object dataToken,
                                                                      IDictionary<string, object> expectedDictionary)
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Act
            routeBuilder.MapRoute("mockName",
                                  "{controller}/{action}",
                                  defaults: null,
                                  constraints: null,
                                  dataTokens: dataToken);

            // Assert
            var templateRoute = (TemplateRoute)routeBuilder.Routes[0];

            // Assert
            Assert.Equal(expectedDictionary.Count, templateRoute.DataTokens.Count);
            foreach (var expectedKey in expectedDictionary.Keys)
            {
                Assert.True(templateRoute.DataTokens.ContainsKey(expectedKey));
                Assert.Equal(expectedDictionary[expectedKey], templateRoute.DataTokens[expectedKey]);
            }
        }

        [Fact]
        public void RegisteringRouteWithInvalidConstraints_Throws()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => routeBuilder.MapRoute("mockName",
                    "{controller}/{action}",
                    defaults: null,
                    constraints: new { controller = "a.*", action = 17 }),
                "The constraint entry 'action' - '17' on the route '{controller}/{action}' " +
                "must have a string value or be of a type which implements '" +
                typeof(IRouteConstraint) + "'.");
        }

        [Fact]
        public void RegisteringRouteWithTwoConstraints()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            var mockConstraint = new Mock<IRouteConstraint>().Object;

            routeBuilder.MapRoute("mockName",
                "{controller}/{action}",
                defaults: null,
                constraints: new { controller = "a.*", action = mockConstraint });

            var constraints = ((TemplateRoute)routeBuilder.Routes[0]).Constraints;

            // Assert
            Assert.Equal(2, constraints.Count);
            Assert.IsType<RegexRouteConstraint>(constraints["controller"]);
            Assert.Equal(mockConstraint, constraints["action"]);
        }

        [Fact]
        public void RegisteringRouteWithOneInlineConstraintAndOneUsingConstraintArgument()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Act
            routeBuilder.MapRoute("mockName",
                "{controller}/{action}/{id:int}",
                defaults: null,
                constraints: new { id = "1*" });

            // Assert
            var constraints = ((TemplateRoute)routeBuilder.Routes[0]).Constraints;
            Assert.Equal(1, constraints.Count);
            var constraint = (CompositeRouteConstraint)constraints["id"];
            Assert.IsType<CompositeRouteConstraint>(constraint);
            Assert.IsType<RegexRouteConstraint>(constraint.Constraints.ElementAt(0));
            Assert.IsType<IntRouteConstraint>(constraint.Constraints.ElementAt(1));
        }

        [Fact]
        public void RegisteringRoute_WithOneInlineConstraint_AddsItToConstraintCollection()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Act
            routeBuilder.MapRoute("mockName",
                "{controller}/{action}/{id:int}",
                defaults: null,
                constraints: null);

            // Assert
            var constraints = ((TemplateRoute)routeBuilder.Routes[0]).Constraints;
            Assert.Equal(1, constraints.Count);
            Assert.IsType<IntRouteConstraint>(constraints["id"]);
        }

        [Fact]
        public void RegisteringRouteWithRouteName_WithNullDefaults_AddsTheRoute()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            routeBuilder.MapRoute(name: "RouteName", template: "{controller}/{action}", defaults: null);

            // Act
            var name = ((TemplateRoute)routeBuilder.Routes[0]).Name;

            // Assert
            Assert.Equal("RouteName", name);
        }

        [Fact]
        public void RegisteringRouteWithRouteName_WithNullDefaultsAndConstraints_AddsTheRoute()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            routeBuilder.MapRoute(name: "RouteName",
                                template: "{controller}/{action}",
                                defaults: null,
                                constraints: null);

            // Act
            var name = ((TemplateRoute)routeBuilder.Routes[0]).Name;

            // Assert
            Assert.Equal("RouteName", name);
        }

        #endregion

        private static IRouteBuilder CreateRouteBuilder()
        {
            var routeBuilder = new RouteBuilder();

            routeBuilder.DefaultHandler = new Mock<IRouter>().Object;

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(o => o.GetService(typeof(IInlineConstraintResolver)))
                               .Returns(_inlineConstraintResolver);
            routeBuilder.ServiceProvider = serviceProviderMock.Object;

            return routeBuilder;
        }

        private static TemplateRoute CreateRoute(string template, bool accept = true)
        {
            return new TemplateRoute(CreateTarget(accept), template, _inlineConstraintResolver);
        }

        private static TemplateRoute CreateRoute(string template,
                                                 object defaults,
                                                 bool accept = true,
                                                 object constraints = null,
                                                 object dataTokens = null)
        {
            return new TemplateRoute(CreateTarget(accept),
                                     template,
                                     new RouteValueDictionary(defaults),
                                     (constraints as IDictionary<string, object>) ??
                                            new RouteValueDictionary(constraints),
                                     (dataTokens as IDictionary<string, object>) ??
                                            new RouteValueDictionary(dataTokens),
                                     _inlineConstraintResolver);
        }

        private static TemplateRoute CreateRoute(IRouter target, string template)
        {
            return new TemplateRoute(target,
                                     template,
                                     new RouteValueDictionary(),
                                     constraints: null,
                                     dataTokens: null,
                                     inlineConstraintResolver: _inlineConstraintResolver);
        }

        private static TemplateRoute CreateRoute(IRouter target, string template, object defaults)
        {
            return new TemplateRoute(target,
                                     template,
                                     new RouteValueDictionary(defaults),
                                     constraints: null,
                                     dataTokens: null,
                                     inlineConstraintResolver: _inlineConstraintResolver);
        }

        private static IRouter CreateTarget(bool accept = true)
        {
            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(c => c.IsBound = accept)
                .Returns<VirtualPathContext>(rc => null);

            target
                .Setup(e => e.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>((c) => c.IsHandled = accept)
                .Returns(Task.FromResult<object>(null));

            return target.Object;
        }

        private static IInlineConstraintResolver GetInlineConstraintResolver()
        {
            var resolverMock = new Mock<IInlineConstraintResolver>();
            resolverMock.Setup(o => o.ResolveConstraint("int")).Returns(new IntRouteConstraint());
            resolverMock.Setup(o => o.ResolveConstraint("range(1,20)")).Returns(new RangeRouteConstraint(1, 20));
            resolverMock.Setup(o => o.ResolveConstraint("alpha")).Returns(new AlphaRouteConstraint());

            return resolverMock.Object;
        }

        private class CapturingConstraint : IRouteConstraint
        {
            public IDictionary<string, object> Values { get; private set; }

            public bool Match(
                HttpContext httpContext,
                IRouter route,
                string routeKey,
                IDictionary<string, object> values,
                RouteDirection routeDirection)
            {
                Values = new RouteValueDictionary(values);
                return true;
            }
        }
    }
}
#endif
