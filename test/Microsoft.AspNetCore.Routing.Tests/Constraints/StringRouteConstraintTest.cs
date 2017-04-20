// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    public class StringRouteConstraintTest
    {
        [Fact]
        public void StringRouteConstraintSimpleTrueWithRouteDirectionIncomingRequestTest()
        {
            // Arrange
            var constraint = new StringRouteConstraint("home");

            // Act
            var values = new RouteValueDictionary(new { controller = "home" });

            var match = constraint.Match(
                new DefaultHttpContext(),
                route: new Mock<IRouter>().Object,
                routeKey: "controller",
                values: values,
                routeDirection: RouteDirection.IncomingRequest);

            // Assert
            Assert.True(match);
        }

        [Fact]
        public void StringRouteConstraintSimpleTrueWithRouteDirectionUrlGenerationTest()
        {
            // Arrange
            var constraint = new StringRouteConstraint("home");

            // Act
            var values = new RouteValueDictionary(new { controller = "home" });

            var match = constraint.Match(
              new DefaultHttpContext(),
              route: new Mock<IRouter>().Object,
              routeKey: "controller",
              values: values,
              routeDirection: RouteDirection.UrlGeneration);

            // Assert
            Assert.True(match);
        }

        [Fact]
        public void StringRouteConstraintSimpleFalseWithRouteDirectionIncomingRequestTest()
        {
            // Arrange
            var constraint = new StringRouteConstraint("admin");

            // Act
            var values = new RouteValueDictionary(new { controller = "home" });

            var match = constraint.Match(
                new DefaultHttpContext(),
                route: new Mock<IRouter>().Object,
                routeKey: "controller",
                values: values,
                routeDirection: RouteDirection.IncomingRequest);

            // Assert
            Assert.False(match);
        }

        [Fact]
        public void StringRouteConstraintSimpleFalseWithRouteDirectionUrlGenerationTest()
        {
            // Arrange
            var constraint = new StringRouteConstraint("admin");

            // Act
            var values = new RouteValueDictionary(new { controller = "home" });

            var match = constraint.Match(
                new DefaultHttpContext(),
                route: new Mock<IRouter>().Object,
                routeKey: "controller",
                values: values,
                routeDirection: RouteDirection.UrlGeneration);

            // Assert
            Assert.False(match);
        }

        [Fact]
        public void StringRouteConstraintKeyNotFoundWithRouteDirectionIncomingRequestTest()
        {
            // Arrange
            var constraint = new StringRouteConstraint("admin");

            // Act
            var values = new RouteValueDictionary(new { controller = "admin" });

            var match = constraint.Match(
                new DefaultHttpContext(),
                route: new Mock<IRouter>().Object,
                routeKey: "action",
                values: values,
                routeDirection: RouteDirection.IncomingRequest);

            // Assert
            Assert.False(match);
        }

        [Fact]
        public void StringRouteConstraintKeyNotFoundWithRouteDirectionUrlGenerationTest()
        {
            // Arrange
            var constraint = new StringRouteConstraint("admin");

            // Act
            var values = new RouteValueDictionary(new { controller = "admin" });

            var match = constraint.Match(
                new DefaultHttpContext(),
                route: new Mock<IRouter>().Object,
                routeKey: "action",
                values: values,
                routeDirection: RouteDirection.UrlGeneration);

            // Assert
            Assert.False(match);
        }

        [Theory]
        [InlineData("User", "uSer", true)]
        [InlineData("User.Admin", "User.Admin", true)]
        [InlineData(@"User\Admin", "User\\Admin", true)]
        [InlineData(null, "user", false)]
        public void StringRouteConstraintEscapingCaseSensitiveAndRouteNullTest(string routeValue, string constraintValue, bool expected)
        {
            // Arrange
            var constraint = new StringRouteConstraint(constraintValue);

            // Act
            var values = new RouteValueDictionary(new { controller = routeValue });

            var match = constraint.Match(
                new DefaultHttpContext(),
                route: new Mock<IRouter>().Object,
                routeKey: "controller",
                values: values,
                routeDirection: RouteDirection.IncomingRequest);

            // Assert
            Assert.Equal(expected, match);
        }
    }
}