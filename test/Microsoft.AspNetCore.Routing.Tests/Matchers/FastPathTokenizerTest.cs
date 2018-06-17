// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public unsafe class FastPathTokenizerTest
    {
        [Fact] // Note: tokenizing a truly empty string is undefined.
        public void Tokenize_EmptyPath()
        {
            // Arrange
            var segments = stackalloc PathSegment[32];

            // Act
            var count = FastPathTokenizer.Tokenize("/", segments, 1);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void Tokenize_SingleSegment()
        {
            // Arrange
            var segments = stackalloc PathSegment[32];

            // Act
            var count = FastPathTokenizer.Tokenize("/abc", segments, 1);

            // Assert
            Assert.Equal(1, count);
            Assert.Equal(new PathSegment(1, 3), segments[0]);
        }

        [Fact]
        public void Tokenize_WithSomeSegments()
        {
            // Arrange
            var segments = stackalloc PathSegment[32];

            // Act
            var count = FastPathTokenizer.Tokenize("/a/b/c", segments, 3);

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
            var segments = stackalloc PathSegment[32];

            // Act
            var count = FastPathTokenizer.Tokenize("/a/b/c/", segments, 3);

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
            var segments = stackalloc PathSegment[32];

            // Act
            var count = FastPathTokenizer.Tokenize("/aaa/bb/ccccc", segments, 3);

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
            var segments = stackalloc PathSegment[32];

            // Act
            var count = FastPathTokenizer.Tokenize("///c", segments, 3);

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
            var segments = stackalloc PathSegment[32];

            // Act
            var count = FastPathTokenizer.Tokenize("/a/b/c/d", segments, 3);

            // Assert
            Assert.Equal(3, count);
            Assert.Equal(new PathSegment(1, 1), segments[0]);
            Assert.Equal(new PathSegment(3, 1), segments[1]);
            Assert.Equal(new PathSegment(5, 1), segments[2]);
        }
    }
}
