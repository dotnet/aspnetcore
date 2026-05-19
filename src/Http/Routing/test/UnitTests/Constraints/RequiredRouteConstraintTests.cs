// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Moq;

namespace Microsoft.AspNetCore.Routing.Tests;

public class RequiredRouteConstraintTests
{
    [Theory]
    [InlineData(RouteDirection.IncomingRequest)]
    [InlineData(RouteDirection.UrlGeneration)]
    public void RequiredRouteConstraint_NoValue(RouteDirection direction)
    {
        // Arrange
        var constraint = new RequiredRouteConstraint();

        // Act
        var result = constraint.Match(
            new DefaultHttpContext(),
            Mock.Of<IRouter>(),
            "area",
            new RouteValueDictionary(new { controller = "Home", action = "Index" }),
            direction);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(RouteDirection.IncomingRequest)]
    [InlineData(RouteDirection.UrlGeneration)]
    public void RequiredRouteConstraint_Null(RouteDirection direction)
    {
        // Arrange
        var constraint = new RequiredRouteConstraint();

        // Act
        var result = constraint.Match(
            new DefaultHttpContext(),
            Mock.Of<IRouter>(),
            "area",
            new RouteValueDictionary(new { controller = "Home", action = "Index", area = (string)null }),
            direction);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(RouteDirection.IncomingRequest)]
    [InlineData(RouteDirection.UrlGeneration)]
    public void RequiredRouteConstraint_EmptyString(RouteDirection direction)
    {
        // Arrange
        var constraint = new RequiredRouteConstraint();

        // Act
        var result = constraint.Match(
            new DefaultHttpContext(),
            Mock.Of<IRouter>(),
            "area",
            new RouteValueDictionary(new { controller = "Home", action = "Index", area = string.Empty }),
            direction);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(RouteDirection.IncomingRequest)]
    [InlineData(RouteDirection.UrlGeneration)]
    public void RequiredRouteConstraint_WithValue(RouteDirection direction)
    {
        // Arrange
        var constraint = new RequiredRouteConstraint();

        // Act
        var result = constraint.Match(
            new DefaultHttpContext(),
            Mock.Of<IRouter>(),
            "area",
            new RouteValueDictionary(new { controller = "Home", action = "Index", area = "Store" }),
            direction);

        // Assert
        Assert.True(result);
    }
}
