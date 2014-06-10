// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45

using System;
using Microsoft.AspNet.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class MaxLengthRouteConstraintTests
    {
        [Theory]
        [InlineData(3, "", true)]
        [InlineData(3, "12", true)]
        [InlineData(3, "123", true)]
        [InlineData(3, "1234", false)]
        public void MaxLengthRouteConstraint_ApplyConstraint(int min, string parameterValue, bool expected)
        {
            // Arrange
            var constraint = new MaxLengthRouteConstraint(min);

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MaxLengthRouteConstraint_SettingMaxLengthLessThanZero_Throws()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(()=> new MaxLengthRouteConstraint(-1));
            Assert.Equal("Value must be greater than or equal to 0.\r\nParameter name: maxLength\r\n" +
                          "Actual value was -1.",
                          ex.Message);
        }
    }
}

#endif