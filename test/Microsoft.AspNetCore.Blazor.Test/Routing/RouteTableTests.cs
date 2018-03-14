// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Blazor.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Test.Routing
{
    public class RouteTableTests
    {
        [Fact]
        public void CanMatchRootTemplate()
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder().AddRoute("/").Build();
            var context = new RouteContext("/");

            // Act
            routeTable.Route(context);

            // Assert
            Assert.NotNull(context.Handler);
        }

        [Fact]
        public void CanMatchLiteralTemplate()
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder().AddRoute("/literal").Build();
            var context = new RouteContext("/literal/");

            // Act
            routeTable.Route(context);

            // Assert
            Assert.NotNull(context.Handler);
        }

        [Fact]
        public void CanMatchTemplateWithMultipleLiterals()
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder().AddRoute("/some/awesome/route/").Build();
            var context = new RouteContext("/some/awesome/route");

            // Act
            routeTable.Route(context);

            // Assert
            Assert.NotNull(context.Handler);
        }

        [Fact]
        public void RouteMatchingIsCaseInsensitive()
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder().AddRoute("/some/AWESOME/route/").Build();
            var context = new RouteContext("/Some/awesome/RouTe");

            // Act
            routeTable.Route(context);

            // Assert
            Assert.NotNull(context.Handler);
        }

        [Fact]
        public void DoesNotMatchIfSegmentsDontMatch()
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder().AddRoute("/some/AWESOME/route/").Build();
            var context = new RouteContext("/some/brilliant/route");

            // Act
            routeTable.Route(context);

            // Assert
            Assert.Null(context.Handler);
        }

        [Theory]
        [InlineData("/some")]
        [InlineData("/some/awesome/route/with/extra/segments")]
        public void DoesNotMatchIfDifferentNumberOfSegments(string path)
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder().AddRoute("/some/awesome/route/").Build();
            var context = new RouteContext(path);

            // Act
            routeTable.Route(context);

            // Assert
            Assert.Null(context.Handler);
        }

        [Theory]
        [InlineData("/value1", "value1")]
        [InlineData("/value2/", "value2")]
        public void CanMatchParameterTemplate(string path, string expectedValue)
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder().AddRoute("/{parameter}").Build();
            var context = new RouteContext(path);

            // Act
            routeTable.Route(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Single(context.Parameters, p => p.Key == "parameter" && p.Value == expectedValue);
        }

        [Fact]
        public void CanMatchTemplateWithMultipleParameters()
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder().AddRoute("/{some}/awesome/{route}/").Build();
            var context = new RouteContext("/an/awesome/path");

            var expectedParameters = new Dictionary<string, string>
            {
                ["some"] = "an",
                ["route"] = "path"
            };

            // Act
            routeTable.Route(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Equal(expectedParameters, context.Parameters);
        }

        [Fact]
        public void PrefersLiteralTemplateOverTemplateWithParameters()
        {
            // Arrange
            var routeTable = new TestRouteTableBuilder()
                .AddRoute("/an/awesome/path")
                .AddRoute("/{some}/awesome/{route}/").Build();
            var context = new RouteContext("/an/awesome/path");

            // Act
            routeTable.Route(context);

            // Assert
            Assert.NotNull(context.Handler);
            Assert.Null(context.Parameters);
        }

        [Fact]
        public void PrefersShorterRoutesOverLongerRoutes()
        {
            // Arrange & Act
            var handler = typeof(int);
            var routeTable = new TestRouteTableBuilder()
                .AddRoute("/an/awesome/path")
                .AddRoute("/an/awesome/", handler).Build();

            // Act
            Assert.Equal("an/awesome", routeTable.Routes[0].Template.TemplateText);
        }

        [Fact]
        public void ProducesAStableOrderForNonAmbiguousRoutes()
        {
            // Arrange & Act
            var handler = typeof(int);
            var routeTable = new TestRouteTableBuilder()
                .AddRoute("/an/awesome/", handler)
                .AddRoute("/a/brilliant/").Build();

            // Act
            Assert.Equal("a/brilliant", routeTable.Routes[0].Template.TemplateText);
        }

        [Theory]
        [InlineData("/literal", "/Literal/")]
        [InlineData("/{parameter}", "/{parameter}/")]
        [InlineData("/literal/{parameter}", "/Literal/{something}")]
        [InlineData("/{parameter}/literal/{something}", "{param}/Literal/{else}")]
        public void DetectsAmbigousRoutes(string left, string right)
        {
            // Arrange
            var expectedMessage = $@"The following routes are ambiguous:
'{left.Trim('/')}' in '{typeof(object).FullName}'
'{right.Trim('/')}' in '{typeof(object).FullName}'
";
            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => new TestRouteTableBuilder()
                .AddRoute(left)
                .AddRoute(right).Build());

            Assert.Equal(expectedMessage, exception.Message);
        }

        private class TestRouteTableBuilder
        {
            IList<(string, Type)> _routeTemplates = new List<(string, Type)>();
            Type _handler = typeof(object);

            public TestRouteTableBuilder AddRoute(string template, Type handler = null)
            {
                _routeTemplates.Add((template, handler ?? _handler));
                return this;
            }

            public RouteTable Build() => new RouteTable(_routeTemplates
                .Select(rt => new RouteEntry(TemplateParser.ParseTemplate(rt.Item1), rt.Item2))
                .OrderBy(id => id, RouteTable.RoutePrecedence)
                .ToArray());
        }
    }
}
