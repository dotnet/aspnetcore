// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Components.LegacyRouteMatching
{
    public class LegacyRouteConstraintTest
    {
        [Fact]
        public void Parse_CreatesDifferentConstraints_ForDifferentKinds()
        {
            // Arrange
            var original = LegacyRouteConstraint.Parse("ignore", "ignore", "int");

            // Act
            var another = LegacyRouteConstraint.Parse("ignore", "ignore", "guid");

            // Assert
            Assert.NotSame(original, another);
        }

        [Fact]
        public void Parse_CachesCreatedConstraint_ForSameKind()
        {
            // Arrange
            var original = LegacyRouteConstraint.Parse("ignore", "ignore", "int");

            // Act
            var another = LegacyRouteConstraint.Parse("ignore", "ignore", "int");

            // Assert
            Assert.Same(original, another);
        }

        [Fact]
        public void Parse_DoesNotThrowIfOptionalConstraint()
        {
            // Act
            var exceptions = Record.Exception(() => LegacyRouteConstraint.Parse("ignore", "ignore", "int?"));

            // Assert
            Assert.Null(exceptions);
        }
    }
}
