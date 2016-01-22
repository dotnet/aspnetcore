// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
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
}
