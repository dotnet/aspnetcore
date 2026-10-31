// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Moq;

namespace Microsoft.AspNetCore.Routing.Constraints;

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
