// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing.Tests;

public class BoolRouteConstraintTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("TruE", true)]
    [InlineData("false", true)]
    [InlineData("FalSe", true)]
    [InlineData(" FalSe", true)]
    [InlineData("True ", true)]
    [InlineData(" False ", true)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(1, false)]
    [InlineData("not-parseable-as-bool", false)]
    public void BoolRouteConstraint(object parameterValue, bool expected)
    {
        // Arrange
        var constraint = new BoolRouteConstraint();

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }
}
