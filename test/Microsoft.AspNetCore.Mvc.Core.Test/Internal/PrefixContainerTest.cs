// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class PrefixContainerTest
    {
        [Fact]
        public void ContainsPrefix_EmptyCollection_EmptyString_False()
        {
            // Arrange
            var keys = new string[] { };
            var container = new PrefixContainer(keys);

            // Act
            var result = container.ContainsPrefix(string.Empty);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ContainsPrefix_HasEntries_EmptyString_True()
        {
            // Arrange
            var keys = new string[] { "some.prefix" };
            var container = new PrefixContainer(keys);

            // Act
            var result = container.ContainsPrefix(string.Empty);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("b")]
        [InlineData("c")]
        [InlineData("d")]
        public void ContainsPrefix_HasEntries_ExactMatch(string prefix)
        {
            // Arrange
            var keys = new string[] { "a", "b", "c", "d" };
            var container = new PrefixContainer(keys);

            // Act
            var result = container.ContainsPrefix(prefix);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("b")]
        [InlineData("c")]
        [InlineData("d")]
        public void ContainsPrefix_HasEntries_NoMatch(string prefix)
        {
            // Arrange
            var keys = new string[] { "ax", "bx", "cx", "dx" };
            var container = new PrefixContainer(keys);

            // Act
            var result = container.ContainsPrefix(prefix);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("b")]
        [InlineData("c")]
        [InlineData("d")]
        public void ContainsPrefix_HasEntries_PrefixMatch_WithDot(string prefix)
        {
            // Arrange
            var keys = new string[] { "a.x", "b.x", "c.x", "d.x" };
            var container = new PrefixContainer(keys);

            // Act
            var result = container.ContainsPrefix(prefix);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("b")]
        [InlineData("c")]
        [InlineData("d")]
        public void ContainsPrefix_HasEntries_PrefixMatch_WithSquareBrace(string prefix)
        {
            // Arrange
            var keys = new string[] { "a[x", "b[x", "c[x", "d[x" };
            var container = new PrefixContainer(keys);

            // Act
            var result = container.ContainsPrefix(prefix);

            // Assert
            Assert.True(result);
        }
    }
}
