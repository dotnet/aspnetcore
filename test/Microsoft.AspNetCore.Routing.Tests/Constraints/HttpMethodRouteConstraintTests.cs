// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    public class HttpMethodRouteConstraintTests
    {
        [Theory]
        [InlineData("GET")]
        [InlineData("PosT")]
        public void HttpMethodRouteConstraint_IncomingRequest_AcceptsAllowedMethods(string httpMethod)
        {
            // Arrange
            var constraint = new HttpMethodRouteConstraint("GET", "post");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = httpMethod;
            var route = Mock.Of<IRouter>();

            var values = new RouteValueDictionary(new { });

            // Act
            var result = constraint.Match(httpContext, route, "httpMethod", values, RouteDirection.IncomingRequest);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("OPTIONS")]
        [InlineData("SomeRandomThing")]
        public void HttpMethodRouteConstraint_IncomingRequest_RejectsOtherMethods(string httpMethod)
        {
            // Arrange
            var constraint = new HttpMethodRouteConstraint("GET", "post");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = httpMethod;
            var route = Mock.Of<IRouter>();

            var values = new RouteValueDictionary(new { });

            // Act
            var result = constraint.Match(httpContext, route, "httpMethod", values, RouteDirection.IncomingRequest);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PosT")]
        public void HttpMethodRouteConstraint_UrlGeneration_AcceptsAllowedMethods(string httpMethod)
        {
            // Arrange
            var constraint = new HttpMethodRouteConstraint("GET", "post");

            var httpContext = new DefaultHttpContext();
            var route = Mock.Of<IRouter>();

            var values = new RouteValueDictionary(new { httpMethod = httpMethod });

            // Act
            var result = constraint.Match(httpContext, route, "httpMethod", values, RouteDirection.UrlGeneration);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("OPTIONS")]
        [InlineData("SomeRandomThing")]
        public void HttpMethodRouteConstraint_UrlGeneration_RejectsOtherMethods(string httpMethod)
        {
            // Arrange
            var constraint = new HttpMethodRouteConstraint("GET", "post");

            var httpContext = new DefaultHttpContext();
            var route = Mock.Of<IRouter>();

            var values = new RouteValueDictionary(new { httpMethod = httpMethod });

            // Act
            var result = constraint.Match(httpContext, route, "httpMethod", values, RouteDirection.UrlGeneration);

            // Assert
            Assert.False(result);
        }
    }
}
