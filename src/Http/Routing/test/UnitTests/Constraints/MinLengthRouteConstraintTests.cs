// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Routing.Tests;

public class MinLengthRouteConstraintTests
{
    [Theory]
    [InlineData(3, "1234", true)]
    [InlineData(3, "123", true)]
    [InlineData(3, "12", false)]
    [InlineData(3, "", false)]
    public void MinLengthRouteConstraint_ApplyConstraint(int min, string parameterValue, bool expected)
    {
        // Arrange
        var constraint = new MinLengthRouteConstraint(min);

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinLengthRouteConstraint_SettingMinLengthLessThanZero_Throws()
    {
        // Arrange
        var expectedMessage = "Value must be greater than or equal to 0.";

        // Act & Assert
        ExceptionAssert.ThrowsArgumentOutOfRange(
            () => new MinLengthRouteConstraint(-1),
            "minLength",
            expectedMessage,
            -1);
    }
}
