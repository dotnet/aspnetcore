// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Constraints;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Template.Tests
{
    public class TemplateRouteTests
    {
        private static IInlineConstraintResolver _inlineConstraintResolver = GetInlineConstraintResolver();

        #region Route Matching

        // PathString in HttpAbstractions guarantees a leading slash - so no value in testing other cases.
        [Fact]
        public async void Match_Success_LeadingSlash()
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
        public async void Match_Success_RootUrl()
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
        public async void Match_Success_Defaults()
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
        public async void Match_Fails()
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
        public async void Match_RejectedByHandler()
        {
            // Arrange
            var route = CreateRoute("{controller}", accept: false);
            var context = CreateRouteContext("/Home");

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.False(context.IsHandled);

            // Issue #16 tracks this.
            Assert.NotNull(context.RouteData.Values);
        }

        private static RouteContext CreateRouteContext(string requestPath)
        {
            var request = new Mock<HttpRequest>(MockBehavior.Strict);
            request.SetupGet(r => r.Path).Returns(new PathString(requestPath));

            var context = new Mock<HttpContext>(MockBehavior.Strict);
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
            var context = CreateVirtualPathContext(new { action = "Store" }, new { Controller = "Home", action = "Blog"});

            var expectedValues = new RouteValueDictionary(new {controller = "Home", action = "Store"});

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

            var route = CreateRoute(target.Object, "Admin/{controller}/{action}", new {area = "Admin"});
            var context = CreateVirtualPathContext(new { action = "Store" }, new { Controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(new { controller = "Home", action = "Store", area = "Admin" });

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
            var context = CreateVirtualPathContext(new { action = "Store", id = 5 }, new { Controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(new { controller = "Home", action = "Store" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.Equal("Home/Store?id=5", path);
            Assert.Equal(expectedValues, childContext.ProvidedValues);
        }

        private static VirtualPathContext CreateVirtualPathContext(object values)
        {
            return CreateVirtualPathContext(new RouteValueDictionary(values), null);
        }

        private static VirtualPathContext CreateVirtualPathContext(object values, object ambientValues)
        {
            return CreateVirtualPathContext(new RouteValueDictionary(values), new RouteValueDictionary(ambientValues));
        }

        private static VirtualPathContext CreateVirtualPathContext(IDictionary<string, object> values, IDictionary<string, object> ambientValues)
        {
            var context = new Mock<HttpContext>(MockBehavior.Strict);

            return new VirtualPathContext(context.Object, ambientValues, values);
        }

        private static VirtualPathContext CreateVirtualPathContext(string routeName)
        {
            return new VirtualPathContext(null, null, null, routeName);
        }

        #endregion

#region Route Registration

        [Fact]
        public void RegisteringRouteWithInvalidConstraints_Throws()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => routeBuilder.MapRoute("mockName", 
                "{controller}/{action}",
                defaults: null,
                constraints: new { controller = "a.*", action = new Object() }),
                "The constraint entry 'action' on the route with route template '{controller}/{action}' " +
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
            Assert.IsType<RegexConstraint>(constraints["controller"]);
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
            Assert.IsType<RegexConstraint>(constraint.Constraints.ElementAt(0));
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

        private static TemplateRoute CreateRoute(string template, object defaults, bool accept = true, IDictionary<string, object> constraints = null)
        {
            return new TemplateRoute(CreateTarget(accept),
                                     template,
                                     new RouteValueDictionary(defaults),
                                     constraints,
                                     _inlineConstraintResolver);
        }

        private static TemplateRoute CreateRoute(IRouter target, string template)
        {
            return new TemplateRoute(target,
                                     template,
                                     new RouteValueDictionary(),
                                     constraints: null,
                                     inlineConstraintResolver: _inlineConstraintResolver);
        }

        private static TemplateRoute CreateRoute(IRouter target, string template, object defaults)
        {
            return new TemplateRoute(target,
                                     template,
                                     new RouteValueDictionary(defaults),
                                     constraints: null,
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
            return resolverMock.Object;
        }
    }
}

#endif
