// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Components.Routing
{
    public class RouteConstraintTest
    {
        [Fact]
        public void Parse_CreatesDifferentConstraints_ForDifferentKinds()
        {
            // Arrange
            var original = RouteConstraint.Parse("ignore", "ignore", "int");

            // Act
            var another = RouteConstraint.Parse("ignore", "ignore", "guid");

            // Assert
            Assert.NotSame(original, another);
        }

        [Fact]
        public void Parse_CachesCreatedConstraint_ForSameKind()
        {
            // Arrange
            var original = RouteConstraint.Parse("ignore", "ignore", "int");

            // Act
            var another = RouteConstraint.Parse("ignore", "ignore", "int");

            // Assert
            Assert.Same(original, another);
        }
    }
}
