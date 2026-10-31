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

    [Fact]
    public void Tokenize_LongPathWithManySegments()
    {
        // Arrange
        var path = "/api/products/categories/1234/items/5678/details/extra-long-segment-name/x/y/z/";
        Span<PathSegment> segments = stackalloc PathSegment[32];

        // Act
        var count = FastPathTokenizer.Tokenize(path, segments);

        // Assert
        var expected = path.Substring(1).TrimEnd('/').Split('/');
        Assert.Equal(expected.Length, count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(expected[i], path.Substring(segments[i].Start, segments[i].Length));
        }
    }

    [Fact]
    public void Tokenize_TooManySegments_LongPath()
    {
        // Arrange
        Span<PathSegment> segments = stackalloc PathSegment[3];

        // Act
        var count = FastPathTokenizer.Tokenize("/aaaa/bbbb/cccc/dddd/eeee/ffff", segments);

        // Assert
        Assert.Equal(3, count);
        Assert.Equal(new PathSegment(1, 4), segments[0]);
        Assert.Equal(new PathSegment(6, 4), segments[1]);
        Assert.Equal(new PathSegment(11, 4), segments[2]);
    }

    [Fact]
    public void Tokenize_MatchesReferenceImplementation()
    {
        // Exhaustively compare all paths up to 17 characters made of 'a' and '/' (so that slashes appear
        // at every position, including vector boundaries) against a simple reference implementation.
        var maxCounts = new[] { 1, 2, 3, 32 };
        var chars = new char[17];
        chars[0] = '/';
        Span<PathSegment> actual = stackalloc PathSegment[32];
        Span<PathSegment> expected = stackalloc PathSegment[32];

        for (var length = 1; length <= chars.Length; length++)
        {
            for (var bits = 0; bits < (1 << (length - 1)); bits++)
            {
                for (var i = 1; i < length; i++)
                {
                    chars[i] = (bits & (1 << (i - 1))) != 0 ? '/' : 'a';
                }

                var path = new string(chars, 0, length);
                foreach (var maxCount in maxCounts)
                {
                    var expectedCount = ReferenceTokenize(path, expected.Slice(0, maxCount));
                    var actualCount = FastPathTokenizer.Tokenize(path, actual.Slice(0, maxCount));

                    Assert.True(expectedCount == actualCount, $"Path '{path}' with max count {maxCount}: expected {expectedCount} segments, got {actualCount}.");
                    for (var i = 0; i < expectedCount; i++)
                    {
                        Assert.True(expected[i].Equals(actual[i]), $"Path '{path}' with max count {maxCount}: segment {i} differs.");
                    }
                }
            }
        }
    }

    private static int ReferenceTokenize(string path, Span<PathSegment> segments)
    {
        var count = 0;
        var start = 1;
        int end;
        while ((end = path.IndexOf('/', start)) >= 0 && count < segments.Length)
        {
            segments[count++] = new PathSegment(start, end - start);
            start = end + 1;
        }

        if (path.Length - start > 0 && count < segments.Length)
        {
            segments[count++] = new PathSegment(start, path.Length - start);
        }

        return count;
    }
}
