// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing.Tests;

public class DecimalRouteConstraintTests
{
    public static IEnumerable<object[]> GetDecimalObject
    {
        get
        {
            yield return new object[]
            {
                    2m,
                    true
            };
        }
    }

    [Theory]
    [InlineData("3.14", true)]
    [InlineData("9223372036854775808.9223372036854775808", true)]
    [InlineData("1.79769313486232E+300", false)]
    [InlineData("not-parseable-as-decimal", false)]
    [InlineData(false, false)]
    [MemberData(nameof(GetDecimalObject))]
    public void DecimalRouteConstraint_ApplyConstraint(object parameterValue, bool expected)
    {
        // Arrange
        var constraint = new DecimalRouteConstraint();

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }
}
