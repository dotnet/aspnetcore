// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Routing.Tests;

public class LengthRouteConstraintTests
{
    [Theory]
    [InlineData(3, "123", true)]
    [InlineData(3, "1234", false)]
    [InlineData(0, "", true)]
    public void LengthRouteConstraint_ExactLength_Tests(int length, string parameterValue, bool expected)
    {
        // Arrange
        var constraint = new LengthRouteConstraint(length);

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(3, 5, "12", false)]
    [InlineData(3, 5, "123", true)]
    [InlineData(3, 5, "1234", true)]
    [InlineData(3, 5, "12345", true)]
    [InlineData(3, 5, "123456", false)]
    public void LengthRouteConstraint_Range_Tests(int min, int max, string parameterValue, bool expected)
    {
        // Arrange
        var constraint = new LengthRouteConstraint(min, max);

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LengthRouteConstraint_SettingLengthLessThanZero_Throws()
    {
        // Arrange
        var expectedMessage = "Value must be greater than or equal to 0.";

        // Act & Assert
        ExceptionAssert.ThrowsArgumentOutOfRange(
            () => new LengthRouteConstraint(-1),
            "length",
            expectedMessage,
            -1);
    }

    [Fact]
    public void LengthRouteConstraint_SettingMinLengthLessThanZero_Throws()
    {
        // Arrange
        var expectedMessage = "Value must be greater than or equal to 0.";

        // Act & Assert
        ExceptionAssert.ThrowsArgumentOutOfRange(
            () => new LengthRouteConstraint(-1, 3),
            "minLength",
            expectedMessage,
            -1);
    }

    [Fact]
    public void LengthRouteConstraint_SettingMaxLengthLessThanZero_Throws()
    {
        // Arrange
        var expectedMessage = "Value must be greater than or equal to 0.";

        // Act & Assert
        ExceptionAssert.ThrowsArgumentOutOfRange(
            () => new LengthRouteConstraint(0, -1),
            "maxLength",
            expectedMessage,
            -1);
    }

    [Fact]
    public void LengthRouteConstraint_MinGreaterThanMax_Throws()
    {
        // Arrange
        var expectedMessage = "The value for argument 'minLength' should be less than or equal to the " +
            "value for the argument 'maxLength'.";

        // Arrange Act & Assert
        ExceptionAssert.ThrowsArgumentOutOfRange(
            () => new LengthRouteConstraint(3, 2),
            "minLength",
            expectedMessage,
            3);
    }
}
