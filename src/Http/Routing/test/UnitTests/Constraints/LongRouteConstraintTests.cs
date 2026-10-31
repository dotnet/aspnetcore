// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing.Tests;

public class LongRouteConstraintTests
{
    [Theory]
    [InlineData(42, true)]
    [InlineData(42L, true)]
    [InlineData("42", true)]
    [InlineData("9223372036854775807", true)]
    [InlineData(3.14, false)]
    [InlineData("43.567", false)]
    [InlineData("42a", false)]
    public void LongRouteConstraintTest(object parameterValue, bool expected)
    {
        // Arrange
        var constraint = new LongRouteConstraint();

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }
}
