// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45

using System;
using Microsoft.AspNet.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
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
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new LengthRouteConstraint(-1));
            Assert.Equal("Value must be greater than or equal to 0.\r\nParameter name: length\r\n" +
                          "Actual value was -1.",
                          ex.Message);
        }

        [Fact]
        public void LengthRouteConstraint_SettingMinLengthLessThanZero_Throws()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new LengthRouteConstraint(-1, 3));
            Assert.Equal("Value must be greater than or equal to 0.\r\nParameter name: minLength\r\n"+
                         "Actual value was -1.",
                         ex.Message);
        }

        [Fact]
        public void LengthRouteConstraint_SettingMaxLengthLessThanZero_Throws()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new LengthRouteConstraint(0, -1));
            Assert.Equal("Value must be greater than or equal to 0.\r\nParameter name: maxLength\r\n" +
                        "Actual value was -1.",
                        ex.Message);
        }

        [Fact]
        public void LengthRouteConstraint_MinGreaterThanMax_Throws()
        {
            // Arrange Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new LengthRouteConstraint(3, 2));
            Assert.Equal("The value for argument 'minLength' should be less than or equal to the "+
                         "value for the argument 'maxLength'.\r\nParameter name: minLength\r\nActual value was 3.",
                         ex.Message);
        }
    }
}

#endif