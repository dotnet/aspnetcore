// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing.Tests;

public class MaxRouteConstraintTests
{
    [Theory]
    [InlineData(3, 2, true)]
    [InlineData(3, 3, true)]
    [InlineData(3, 4, false)]
    public void MaxRouteConstraint_ApplyConstraint(long max, int parameterValue, bool expected)
    {
        // Arrange
        var constraint = new MaxRouteConstraint(max);

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }
}
