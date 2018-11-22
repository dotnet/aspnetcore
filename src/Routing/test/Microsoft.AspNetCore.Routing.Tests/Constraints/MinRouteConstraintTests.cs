// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
    public class MinRouteConstraintTests
    {
        [Theory]
        [InlineData(3, 4, true)]
        [InlineData(3, 3, true)]
        [InlineData(3, 2, false)]
        public void MinRouteConstraint_ApplyConstraint(long min, int parameterValue, bool expected)
        {
            // Arrange
            var constraint = new MinRouteConstraint(min);

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
