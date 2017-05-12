// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
    public class DecimalRouteConstraintTests
    {
        public static IEnumerable<object[]> GetDecimalObject
        {
            get
            {
                yield return new object[]
                {
                    2m,
                    true
                };
            }
        }

        [Theory]
        [InlineData("3.14", true)]
        [InlineData("9223372036854775808.9223372036854775808", true)]
        [InlineData("1.79769313486232E+300", false)]
        [InlineData("not-parseable-as-decimal", false)]
        [InlineData(false, false)]
        [MemberData(nameof(GetDecimalObject))]
        public void DecimalRouteConstraint_ApplyConstraint(object parameterValue, bool expected)
        {
            // Arrange
            var constraint = new DecimalRouteConstraint();

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
