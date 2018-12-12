// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tests
{
    public class BoolRouteConstraintTests
    {
        [Theory]
        [InlineData("true", true)]
        [InlineData("TruE", true)]
        [InlineData("false", true)]
        [InlineData("FalSe", true)]
        [InlineData(" FalSe", true)]
        [InlineData("True ", true)]
        [InlineData(" False ", true)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(1, false)]
        [InlineData("not-parseable-as-bool", false)]
        public void BoolRouteConstraint(object parameterValue, bool expected)
        {
            // Arrange
            var constraint = new BoolRouteConstraint();

            // Act
            var actual = ConstraintsTestHelper.TestConstraint(constraint, parameterValue);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
