﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
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
        public void Match_Success_LeadingSlash()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateRouteContext("/Home/Index");

            // Act
            var match = route.Match(context);

            // Assert
            Assert.NotNull(match);
            Assert.Equal(2, match.Values.Count);
            Assert.Equal("Home", match.Values["controller"]);
            Assert.Equal("Index", match.Values["action"]);
        }

        [Fact]
        public void Match_Success_RootUrl()
        {
            // Arrange
            var route = CreateRoute("");
            var context = CreateRouteContext("/");

            // Act
            var match = route.Match(context);

            // Assert
            Assert.NotNull(match);
            Assert.Equal(0, match.Values.Count);
        }

        [Fact]
        public void Match_Success_Defaults()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}", new { action = "Index" });
            var context = CreateRouteContext("/Home");

            // Act
            var match = route.Match(context);

            // Assert
            Assert.NotNull(match);
            Assert.Equal(2, match.Values.Count);
            Assert.Equal("Home", match.Values["controller"]);
            Assert.Equal("Index", match.Values["action"]);
        }

        [Fact]
        public void Match_Fails()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateRouteContext("/Home");

            // Act
            var match = route.Match(context);

            // Assert
            Assert.Null(match);
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
        public void Bind_Success()
        {
            // Arrange
            var route = CreateRoute("{controller}");
            var context = CreateRouteBindContext(new {controller = "Home"});

            // Act
            var bind = route.Bind(context);

            // Assert
            Assert.NotNull(bind);
            Assert.Equal("Home", bind.Url);
        }

        [Fact]
        public void Bind_Fail()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateRouteBindContext(new { controller = "Home" });

            // Act
            var bind = route.Bind(context);

            // Assert
            Assert.Null(bind);
        }

        [Fact]
        public void Bind_Success_AmbientValues()
        {
            // Arrange
            var route = CreateRoute("{controller}/{action}");
            var context = CreateRouteBindContext(new { action = "Index"}, new { controller = "Home" });

            // Act
            var bind = route.Bind(context);

            // Assert
            Assert.NotNull(bind);
            Assert.Equal("Home/Index", bind.Url);
        }

        private static RouteBindContext CreateRouteBindContext(object values)
        {
            return CreateRouteBindContext(new RouteValueDictionary(values), null);
        }

        private static RouteBindContext CreateRouteBindContext(object values, object ambientValues)
        {
            return CreateRouteBindContext(new RouteValueDictionary(values), new RouteValueDictionary(ambientValues));
        }

        private static RouteBindContext CreateRouteBindContext(IDictionary<string, object> values, IDictionary<string, object> ambientValues)
        {
            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.Setup(c => c.GetFeature<IRouteValues>()).Returns(new RouteValues(ambientValues));

            return new RouteBindContext(context.Object, values);
        }

        #endregion

        private static TemplateRoute CreateRoute(string template)
        {
            return new TemplateRoute(CreateEndpoint(), template);
        }

        private static TemplateRoute CreateRoute(string template, object defaults)
        {
            return new TemplateRoute(CreateEndpoint(), template, new RouteValueDictionary(defaults));
        }

        private static IRouteEndpoint CreateEndpoint()
        {
            return new Mock<IRouteEndpoint>(MockBehavior.Strict).Object;
        }
    }
}
