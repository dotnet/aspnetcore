// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing.Tests;

public class DateTimeRouteConstraintTests
{
    public static IEnumerable<object[]> GetDateTimeObject
    {
        get
        {
            yield return new object[]
            {
                    DateTime.Now,
                    true
            };
        }
    }

    [Theory]
    [InlineData("12/25/2009", true)]
    [InlineData("25/12/2009 11:45:00 PM", false)]
    [InlineData("25/12/2009", false)]
    [InlineData("11:45:00 PM", true)]
    [InlineData("11:45:00", true)]
    [InlineData("11:45", true)]
    [InlineData("11", false)]
    [InlineData("", false)]
    [InlineData("Apr 5 2009 11:45:00 PM", true)]
    [InlineData("April 5 2009 11:45:00 PM", true)]
    [InlineData("12/25/2009 11:45:00 PM", true)]
    [InlineData("2009-05-12T11:45:00Z", true)]
    [InlineData("not-parseable-as-date", false)]
    [InlineData(false, false)]
    [MemberData(nameof(GetDateTimeObject))]
    public void DateTimeRouteConstraint(object parameterValue, bool expected)
    {
        // Arrange
        var constraint = new DateTimeRouteConstraint();

        // Act
        var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

        // Assert
        Assert.Equal(expected, actual);
    }
}
