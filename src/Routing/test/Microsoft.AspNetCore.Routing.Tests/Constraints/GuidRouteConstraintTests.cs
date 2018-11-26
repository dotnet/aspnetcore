// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
    public class GuidRouteConstraintTests
    {
        [Theory]
        [InlineData("12345678-1234-1234-1234-123456789012", false, true)]
        [InlineData("12345678-1234-1234-1234-123456789012", true, true)]
        [InlineData("12345678901234567890123456789012", false, true)]
        [InlineData("not-parseable-as-guid", false, false)]
        [InlineData(12, false, false)]

        public void GuidRouteConstraint_ApplyConstraint(object parameterValue, bool parseBeforeTest, bool expected)
        {
            // Arrange
            if (parseBeforeTest)
            {
                parameterValue = Guid.Parse(parameterValue.ToString());
            }

            var constraint = new GuidRouteConstraint();

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
