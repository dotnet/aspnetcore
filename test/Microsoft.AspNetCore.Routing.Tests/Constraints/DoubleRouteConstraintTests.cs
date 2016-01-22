// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
    public class DoubleRouteConstraintTests
    {
        [Theory]
        [InlineData("3.14", true)]
        [InlineData(3.14f, true)]
        [InlineData(3.14d, true)]
        [InlineData("1.79769313486232E+300", true)]
        [InlineData("not-parseable-as-double", false)]
        [InlineData(false, false)]
        public void DoubleRouteConstraint_ApplyConstraint(object parameterValue, bool expected)
        {
            // Arrange
            var constraint = new DoubleRouteConstraint();

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
