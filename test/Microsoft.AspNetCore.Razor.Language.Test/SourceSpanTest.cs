// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class SourceSpanTest
    {
        [Fact]
        public void GeneratedCodeMappingsAreEqualIfDataIsEqual()
        {
            // Arrange
            var left = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 4),
                new SourceSpan(new SourceLocation(5, 6, 7), 8));

            var right = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 4),
                new SourceSpan(new SourceLocation(5, 6, 7), 8));

            // Assert
            Assert.True(left == right);
            Assert.True(left.Equals(right));
            Assert.True(right.Equals(left));
            Assert.True(Equals(left, right));
        }

        [Fact]
        public void GeneratedCodeMappingsAreNotEqualIfCodeLengthIsNotEqual()
        {
            // Arrange
            var left = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 4),
                new SourceSpan(new SourceLocation(5, 6, 7), 8));

            var right = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 5),
                new SourceSpan(new SourceLocation(5, 6, 7), 9));

            // Assert
            AssertNotEqual(left, right);
        }

        [Fact]
        public void GeneratedCodeMappingsAreNotEqualIfStartGeneratedColumnIsNotEqual()
        {
            // Arrange
            var left = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 4),
                new SourceSpan(new SourceLocation(5, 6, 7), 8));

            var right = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 4),
                new SourceSpan(new SourceLocation(5, 6, 8), 8));

            // Assert
            AssertNotEqual(left, right);
        }

        [Fact]
        public void GeneratedCodeMappingsAreNotEqualIfStartColumnIsNotEqual()
        {
            // Arrange
            var left = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 4),
                new SourceSpan(new SourceLocation(5, 6, 8), 8));

            var right = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 4),
                new SourceSpan(new SourceLocation(5, 6, 7), 8));

            // Assert
            AssertNotEqual(left, right);
        }

        [Fact]
        public void GeneratedCodeMappingsAreNotEqualIfStartLineIsNotEqual()
        {
            // Arrange
            var left = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 4),
                new SourceSpan(new SourceLocation(5, 5, 7), 8));

            var right = new LineMapping(
                new SourceSpan(new SourceLocation(1, 1, 3), 4),
                new SourceSpan(new SourceLocation(5, 6, 7), 8));

            // Assert
            AssertNotEqual(left, right);
        }

        [Fact]
        public void GeneratedCodeMappingsAreNotEqualIfAbsoluteIndexIsNotEqual()
        {
            // Arrange
            var left = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 4),
                new SourceSpan(new SourceLocation(4, 6, 7), 8));

            var right = new LineMapping(
                new SourceSpan(new SourceLocation(1, 2, 3), 4),
                new SourceSpan(new SourceLocation(5, 6, 7), 9));

            // Assert
            AssertNotEqual(left, right);
        }

        private void AssertNotEqual(LineMapping left, LineMapping right)
        {
            Assert.False(left == right);
            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));
            Assert.False(Equals(left, right));
        }
    }
}