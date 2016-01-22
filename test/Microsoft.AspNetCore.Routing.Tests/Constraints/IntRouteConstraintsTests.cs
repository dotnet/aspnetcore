// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
    public class IntRouteConstraintsTests
    {
        [Theory]
        [InlineData(42, true)]
        [InlineData("42", true)]
        [InlineData(3.14, false)]
        [InlineData("43.567", false)]
        [InlineData("42a", false)]
        public void IntRouteConstraint_Match_AppliesConstraint(object parameterValue, bool expected)
        {
            // Arrange
            var constraint = new IntRouteConstraint();

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
