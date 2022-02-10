// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing.Tests;

public class MinRouteConstraintTests
{
    [Theory]
    [InlineData(3, 4, true)]
    [InlineData(3, 3, true)]
    [InlineData(3, 2, false)]
    public void MinRouteConstraint_ApplyConstraint(long min, int parameterValue, bool expected)
    {
        // Arrange
        var constraint = new MinRouteConstraint(min);

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }
}
