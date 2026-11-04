// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Routing.Tests;

public class RangeRouteConstraintTests
{
    [Theory]
    [InlineData(long.MinValue, long.MaxValue, 2, true)]
    [InlineData(3, 5, 3, true)]
    [InlineData(3, 5, 4, true)]
    [InlineData(3, 5, 5, true)]
    [InlineData(3, 5, 6, false)]
    [InlineData(3, 5, 2, false)]
    [InlineData(3, 3, 2, false)]
    [InlineData(3, 3, 3, true)]
    public void RangeRouteConstraintTest_ValidValue_ApplyConstraint(long min, long max, int parameterValue, bool expected)
    {
        // Arrange
        var constraint = new RangeRouteConstraint(min, max);

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RangeRouteConstraint_MinGreaterThanMax_Throws()
    {
        // Arrange
        var expectedMessage = "The value for argument 'min' should be less than or equal to the value for the " +
                              "argument 'max'.";

        // Act & Assert
        ExceptionAssert.ThrowsArgumentOutOfRange(
            () => new RangeRouteConstraint(3, 2),
            "min",
            expectedMessage,
            3L);
    }
}
