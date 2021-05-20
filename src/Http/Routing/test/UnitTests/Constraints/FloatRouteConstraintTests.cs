// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
    public class FloatRouteConstraintTests
    {
        [Theory]
        [InlineData("3.14", true)]
        [InlineData(3.14, true)]
        [InlineData("not-parseable-as-float", false)]
        [InlineData(false, false)]
        [InlineData("1.79769313486232E+300", true)] // Parses as infinity
        public void FloatRouteConstraint_ApplyConstraint(object parameterValue, bool expected)
        {
            // Arrange
            var constraint = new FloatRouteConstraint();

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
