// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing.Tests;

public class GuidRouteConstraintTests
{
    [Theory]
    [InlineData("12345678-1234-1234-1234-123456789012", false, true)]
    [InlineData("12345678-1234-1234-1234-123456789012", true, true)]
    [InlineData("12345678901234567890123456789012", false, true)]
    [InlineData("not-parseable-as-guid", false, false)]
    [InlineData(12, false, false)]

    public void GuidRouteConstraint_ApplyConstraint(object parameterValue, bool parseBeforeTest, bool expected)
    {
        // Arrange
        if (parseBeforeTest)
        {
            parameterValue = Guid.Parse(parameterValue.ToString(), CultureInfo.InvariantCulture);
        }

        var constraint = new GuidRouteConstraint();

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }
}
