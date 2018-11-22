// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
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
}
