﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if NET45

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Template.Tests
{
    public class TemplateRouteTests
    {
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
            Assert.Equal(2, context.Values.Count);
            Assert.Equal("Home", context.Values["controller"]);
            Assert.Equal("Index", context.Values["action"]);
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
            Assert.Equal(0, context.Values.Count);
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
            Assert.Equal(2, context.Values.Count);
            Assert.Equal("Home", context.Values["controller"]);
            Assert.Equal("Index", context.Values["action"]);
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
            Assert.NotNull(context.Values);
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
            var context = CreateVirtualPathContext(new {controller = "Home"});

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
            var context = CreateVirtualPathContext(new { action = "Index"}, new { controller = "Home" });

            // Act
            var path = route.GetVirtualPath(context);

            // Assert
            Assert.True(context.IsBound);
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

        private static VirtualPathContext CreateVirtualPathContext(IDictionary<string, object> values, IDictionary<string, object> ambientValues)
        {
            var context = new Mock<HttpContext>(MockBehavior.Strict);

            return new VirtualPathContext(context.Object, ambientValues, values);
        }

        #endregion

        private static TemplateRoute CreateRoute(string template, bool accept = true)
        {
            return new TemplateRoute(CreateTarget(accept), template);
        }

        private static TemplateRoute CreateRoute(string template, object defaults, bool accept = true)
        {
            return new TemplateRoute(CreateTarget(accept), template, new RouteValueDictionary(defaults), null);
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
                .Callback<RouteContext>(async (c) => c.IsHandled = accept)
                .Returns(Task.FromResult<object>(null));

            return target.Object;
        }
    }
}

#endif