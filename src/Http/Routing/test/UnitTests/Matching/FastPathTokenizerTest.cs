// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

public class FastPathTokenizerTest
{
    // Generally this will only happen in tests when the HttpContext hasn't been
    // initialized. We still don't want to crash in this case.
    [Fact]
    public void Tokenize_EmptyString()
    {
        // Arrange
        Span<PathSegment> segments = stackalloc PathSegment[1];

        // Act
        var count = FastPathTokenizer.Tokenize("", segments);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void Tokenize_RootPath()
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
