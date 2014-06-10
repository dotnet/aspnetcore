// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45

using System;
using Microsoft.AspNet.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
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
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MinLengthRouteConstraint(-1));
            Assert.Equal("Value must be greater than or equal to 0.\r\nParameter name: minLength\r\n" +
                          "Actual value was -1.",
                          ex.Message);
        }
    }
}

#endif