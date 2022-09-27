// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

public class RouteConstraintTest
{
    [Fact]
    public void Parse_CreatesDifferentConstraints_ForDifferentKinds()
    {
        // Arrange
        var original = RouteConstraint.Parse("ignore", "ignore", "int");

        // Act
        var another = RouteConstraint.Parse("ignore", "ignore", "guid");

        // Assert
        Assert.NotSame(original, another);
    }

    [Fact]
    public void Parse_CachesCreatedConstraint_ForSameKind()
    {
        // Arrange
        var original = RouteConstraint.Parse("ignore", "ignore", "int");

        // Act
        var another = RouteConstraint.Parse("ignore", "ignore", "int");

        // Assert
        Assert.Same(original, another);
    }
}
