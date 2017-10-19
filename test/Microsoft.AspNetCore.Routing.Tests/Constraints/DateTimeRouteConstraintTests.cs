// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
    public class DateTimeRouteConstraintTests
    {
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
        public void DateTimeRouteConstraint_ParsesStrings(string parameterValue, bool expected)
        {
            // Arrange
            var constraint = new DateTimeRouteConstraint();

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DateTimeRouteConstraint_AcceptsDateTimeObjects_ReturnsTrue()
        {
            // Arrange
            var constraint = new DateTimeRouteConstraint();

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, DateTime.Now);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void DateTimeRouteConstraint_IgnoresOtherTypes_ReturnsFalse()
        {
            // Arrange
            var constraint = new DateTimeRouteConstraint();

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, false);

            // Assert
            Assert.False(actual);
        }
    }
}
