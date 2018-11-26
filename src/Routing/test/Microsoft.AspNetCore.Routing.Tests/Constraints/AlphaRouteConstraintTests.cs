// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
    public class AlphaRouteConstraintTests
    {
        [Theory]
        [InlineData("alpha", true)]
        [InlineData("a1pha", false)]
        [InlineData("ALPHA", true)]
        [InlineData("A1PHA", false)]
        [InlineData("alPHA", true)]
        [InlineData("A1pHA", false)]
        [InlineData("AlpHA╥", false)]
        [InlineData("", true)]
        public void AlphaRouteConstraintTest(string parameterValue, bool expected)
        {
            // Arrange
            var constraint = new AlphaRouteConstraint();

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
