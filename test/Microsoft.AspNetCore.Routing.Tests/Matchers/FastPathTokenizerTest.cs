// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class FastPathTokenizerTest
    {
        [Fact] // Note: tokenizing a truly empty string is undefined.
        public void Tokenize_EmptyPath()
        {
            // Arrange
            Span<PathSegment> segments = stackalloc PathSegment[1];

            // Act
            var count = FastPathTokenizer.Tokenize("/", segments);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void Tokenize_SingleSegment()
        {
            // Arrange
            Span<PathSegment> segments = stackalloc PathSegment[1];

            // Act
            var count = FastPathTokenizer.Tokenize("/abc", segments);

            // Assert
            Assert.Equal(1, count);
            Assert.Equal(new PathSegment(1, 3), segments[0]);
        }

        [Fact]
        public void Tokenize_WithSomeSegments()
        {
            // Arrange
            Span<PathSegment> segments = stackalloc PathSegment[3];

            // Act
            var count = FastPathTokenizer.Tokenize("/a/b/c", segments);

            // Assert
            Assert.Equal(3, count);
            Assert.Equal(new PathSegment(1, 1), segments[0]);
            Assert.Equal(new PathSegment(3, 1), segments[1]);
            Assert.Equal(new PathSegment(5, 1), segments[2]);
        }

        [Fact] // Empty trailing / is ignored
        public void Tokenize_WithSomeSegments_TrailingSlash()
        {
            // Arrange
            Span<PathSegment> segments = stackalloc PathSegment[3];

            // Act
            var count = FastPathTokenizer.Tokenize("/a/b/c/", segments);

            // Assert
            Assert.Equal(3, count);
            Assert.Equal(new PathSegment(1, 1), segments[0]);
            Assert.Equal(new PathSegment(3, 1), segments[1]);
            Assert.Equal(new PathSegment(5, 1), segments[2]);
        }

        [Fact]
        public void Tokenize_LongerSegments()
        {
            // Arrange
            Span<PathSegment> segments = stackalloc PathSegment[3];

            // Act
            var count = FastPathTokenizer.Tokenize("/aaa/bb/ccccc", segments);

            // Assert
            Assert.Equal(3, count);
            Assert.Equal(new PathSegment(1, 3), segments[0]);
            Assert.Equal(new PathSegment(5, 2), segments[1]);
            Assert.Equal(new PathSegment(8, 5), segments[2]);
        }

        [Fact]
        public void Tokenize_EmptySegments()
        {
            // Arrange
            Span<PathSegment> segments = stackalloc PathSegment[3];

            // Act
            var count = FastPathTokenizer.Tokenize("///c", segments);

            // Assert
            Assert.Equal(3, count);
            Assert.Equal(new PathSegment(1, 0), segments[0]);
            Assert.Equal(new PathSegment(2, 0), segments[1]);
            Assert.Equal(new PathSegment(3, 1), segments[2]);
        }

        [Fact]
        public void Tokenize_TooManySegments()
        {
            // Arrange
            Span<PathSegment> segments = stackalloc PathSegment[3];

            // Act
            var count = FastPathTokenizer.Tokenize("/a/b/c/d", segments);

            // Assert
            Assert.Equal(3, count);
            Assert.Equal(new PathSegment(1, 1), segments[0]);
            Assert.Equal(new PathSegment(3, 1), segments[1]);
            Assert.Equal(new PathSegment(5, 1), segments[2]);
        }
    }
}
