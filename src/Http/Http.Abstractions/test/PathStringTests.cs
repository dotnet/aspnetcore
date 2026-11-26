// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Http;

public class PathStringTests
{
    [Fact]
    public void CtorThrows_IfPathDoesNotHaveLeadingSlash()
    {
        // Act and Assert
        ExceptionAssert.ThrowsArgument(() => new PathString("hello"), "value", "The path in 'value' must start with '/'.");
    }

    [Fact]
    public void Equals_EmptyPathStringAndDefaultPathString()
    {
        // Act and Assert
        Assert.Equal(default(PathString), PathString.Empty);
        Assert.Equal(default(PathString), PathString.Empty);
        Assert.True(PathString.Empty == default(PathString));
        Assert.True(default(PathString) == PathString.Empty);
        Assert.True(PathString.Empty.Equals(default(PathString)));
        Assert.True(default(PathString).Equals(PathString.Empty));
    }

    [Fact]
    public void NotEquals_DefaultPathStringAndNonNullPathString()
    {
        // Arrange
        var pathString = new PathString("/hello");

        // Act and Assert
        Assert.NotEqual(default(PathString), pathString);
    }

    [Fact]
    public void NotEquals_EmptyPathStringAndNonNullPathString()
    {
        // Arrange
        var pathString = new PathString("/hello");

        // Act and Assert
        Assert.NotEqual(pathString, PathString.Empty);
    }

    [Fact]
    public void HashCode_CheckNullAndEmptyHaveSameHashcodes()
    {
        Assert.Equal(PathString.Empty.GetHashCode(), default(PathString).GetHashCode());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void AddPathString_HandlesNullAndEmptyStrings(string? appString, string? concatString)
    {
        // Arrange
        var appPath = new PathString(appString);
        var concatPath = new PathString(concatString);

        // Act
        var result = appPath.Add(concatPath);

        // Assert
        Assert.False(result.HasValue);
    }

    [Theory]
    [InlineData("", "/", "/")]
    [InlineData("/", null, "/")]
    [InlineData("/", "", "/")]
    [InlineData("/", "/test", "/test")]
    [InlineData("/myapp/", "/test/bar", "/myapp/test/bar")]
    [InlineData("/myapp/", "/test/bar/", "/myapp/test/bar/")]
    public void AddPathString_HandlesLeadingAndTrailingSlashes(string appString, string? concatString, string expected)
    {
        // Arrange
        var appPath = new PathString(appString);
        var concatPath = new PathString(concatString);

        // Act
        var result = appPath.Add(concatPath);

        // Assert
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void ImplicitStringConverters_WorksWithAdd()
    {
        var scheme = "http";
        var host = new HostString("localhost:80");
        var pathBase = new PathString("/base");
        var path = new PathString("/path");
        var query = new QueryString("?query");
        var fragment = new FragmentString("#frag");

        var result = scheme + "://" + host + pathBase + path + query + fragment;
        Assert.Equal("http://localhost:80/base/path?query#frag", result);

        result = pathBase + path + query + fragment;
        Assert.Equal("/base/path?query#frag", result);

        result = path + "text";
        Assert.Equal("/pathtext", result);
    }

    [Theory]
    [InlineData("/test/path", "/TEST", true)]
    [InlineData("/test/path", "/TEST/pa", false)]
    [InlineData("/TEST/PATH", "/test", true)]
    [InlineData("/TEST/path", "/test/pa", false)]
    [InlineData("/test/PATH/path/TEST", "/TEST/path/PATH", true)]
    public void StartsWithSegments_DoesACaseInsensitiveMatch(string sourcePath, string testPath, bool expectedResult)
    {
        var source = new PathString(sourcePath);
        var test = new PathString(testPath);

        var result = source.StartsWithSegments(test);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("/a/", "/a/", true)]
    [InlineData("/a/b", "/a/", false)]
    [InlineData("/a/b/", "/a/", false)]
    [InlineData("/a//b", "/a/", true)]
    [InlineData("/a//b/", "/a/", true)]
    public void StartsWithSegments_DoesMatchExactPathOrPathWithExtraTrailingSlash(string sourcePath, string testPath, bool expectedResult)
    {
        var source = new PathString(sourcePath);
        var test = new PathString(testPath);

        var result = source.StartsWithSegments(test);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("/test/path", "/TEST", true)]
    [InlineData("/test/path", "/TEST/pa", false)]
    [InlineData("/TEST/PATH", "/test", true)]
    [InlineData("/TEST/path", "/test/pa", false)]
    [InlineData("/test/PATH/path/TEST", "/TEST/path/PATH", true)]
    public void StartsWithSegmentsWithRemainder_DoesACaseInsensitiveMatch(string sourcePath, string testPath, bool expectedResult)
    {
        var source = new PathString(sourcePath);
        var test = new PathString(testPath);

        var result = source.StartsWithSegments(test, out var remaining);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("/a/", "/a/", true)]
    [InlineData("/a/b", "/a/", false)]
    [InlineData("/a/b/", "/a/", false)]
    [InlineData("/a//b", "/a/", true)]
    [InlineData("/a//b/", "/a/", true)]
    public void StartsWithSegmentsWithRemainder_DoesMatchExactPathOrPathWithExtraTrailingSlash(string sourcePath, string testPath, bool expectedResult)
    {
        var source = new PathString(sourcePath);
        var test = new PathString(testPath);

        var result = source.StartsWithSegments(test, out var remaining);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("/test/path", "/TEST", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/test/path", "/TEST", StringComparison.Ordinal, false)]
    [InlineData("/test/path", "/TEST/pa", StringComparison.OrdinalIgnoreCase, false)]
    [InlineData("/test/path", "/TEST/pa", StringComparison.Ordinal, false)]
    [InlineData("/TEST/PATH", "/test", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/TEST/PATH", "/test", StringComparison.Ordinal, false)]
    [InlineData("/TEST/path", "/test/pa", StringComparison.OrdinalIgnoreCase, false)]
    [InlineData("/TEST/path", "/test/pa", StringComparison.Ordinal, false)]
    [InlineData("/test/PATH/path/TEST", "/TEST/path/PATH", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/test/PATH/path/TEST", "/TEST/path/PATH", StringComparison.Ordinal, false)]
    public void StartsWithSegments_DoesMatchUsingSpecifiedComparison(string sourcePath, string testPath, StringComparison comparison, bool expectedResult)
    {
        var source = new PathString(sourcePath);
        var test = new PathString(testPath);

        var result = source.StartsWithSegments(test, comparison);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("/a/", "/a/", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/a/", "/a/", StringComparison.Ordinal, true)]
    [InlineData("/a/b", "/a/", StringComparison.OrdinalIgnoreCase, false)]
    [InlineData("/a/b", "/a/", StringComparison.Ordinal, false)]
    [InlineData("/a/b/", "/a/", StringComparison.OrdinalIgnoreCase, false)]
    [InlineData("/a/b/", "/a/", StringComparison.Ordinal, false)]
    [InlineData("/a//b", "/a/", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/a//b", "/a/", StringComparison.Ordinal, true)]
    [InlineData("/a//b/", "/a/", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/a//b/", "/a/", StringComparison.Ordinal, true)]
    public void StartsWithSegments_DoesMatchExactPathOrPathWithExtraTrailingSlashUsingSpecifiedComparison(string sourcePath, string testPath, StringComparison comparison, bool expectedResult)
    {
        var source = new PathString(sourcePath);
        var test = new PathString(testPath);

        var result = source.StartsWithSegments(test, comparison);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("/test/path", "/TEST", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/test/path", "/TEST", StringComparison.Ordinal, false)]
    [InlineData("/test/path", "/TEST/pa", StringComparison.OrdinalIgnoreCase, false)]
    [InlineData("/test/path", "/TEST/pa", StringComparison.Ordinal, false)]
    [InlineData("/TEST/PATH", "/test", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/TEST/PATH", "/test", StringComparison.Ordinal, false)]
    [InlineData("/TEST/path", "/test/pa", StringComparison.OrdinalIgnoreCase, false)]
    [InlineData("/TEST/path", "/test/pa", StringComparison.Ordinal, false)]
    [InlineData("/test/PATH/path/TEST", "/TEST/path/PATH", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/test/PATH/path/TEST", "/TEST/path/PATH", StringComparison.Ordinal, false)]
    public void StartsWithSegmentsWithRemainder_DoesMatchUsingSpecifiedComparison(string sourcePath, string testPath, StringComparison comparison, bool expectedResult)
    {
        var source = new PathString(sourcePath);
        var test = new PathString(testPath);

        var result = source.StartsWithSegments(test, comparison, out var remaining);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("/a/", "/a/", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/a/", "/a/", StringComparison.Ordinal, true)]
    [InlineData("/a/b", "/a/", StringComparison.OrdinalIgnoreCase, false)]
    [InlineData("/a/b", "/a/", StringComparison.Ordinal, false)]
    [InlineData("/a/b/", "/a/", StringComparison.OrdinalIgnoreCase, false)]
    [InlineData("/a/b/", "/a/", StringComparison.Ordinal, false)]
    [InlineData("/a//b", "/a/", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/a//b", "/a/", StringComparison.Ordinal, true)]
    [InlineData("/a//b/", "/a/", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("/a//b/", "/a/", StringComparison.Ordinal, true)]
    public void StartsWithSegmentsWithRemainder_DoesMatchExactPathOrPathWithExtraTrailingSlashUsingSpecifiedComparison(string sourcePath, string testPath, StringComparison comparison, bool expectedResult)
    {
        var source = new PathString(sourcePath);
        var test = new PathString(testPath);

        var result = source.StartsWithSegments(test, comparison, out var remaining);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    // unreserved
    [InlineData("/abc123.-_~", "/abc123.-_~")]
    // colon
    [InlineData("/:", "/:")]
    // at
    [InlineData("/@", "/@")]
    // sub-delims
    [InlineData("/!$&'()*+,;=", "/!$&'()*+,;=")]
    // reserved
    [InlineData("/?#[]", "/%3F%23%5B%5D")]
    // pct-encoding
    [InlineData("/单行道", "/%E5%8D%95%E8%A1%8C%E9%81%93")]
    // mixed
    [InlineData("/index/单行道=(x*y)[abc]", "/index/%E5%8D%95%E8%A1%8C%E9%81%93=(x*y)%5Babc%5D")]
    [InlineData("/index/单行道=(x*y)[abc]_", "/index/%E5%8D%95%E8%A1%8C%E9%81%93=(x*y)%5Babc%5D_")]
    // encoded
    [InlineData("/http%3a%2f%2f[foo]%3A5000/", "/http%3a%2f%2f%5Bfoo%5D%3A5000/")]
    [InlineData("/http%3a%2f%2f[foo]%3A5000/%", "/http%3a%2f%2f%5Bfoo%5D%3A5000/%25")]
    [InlineData("/http%3a%2f%2f[foo]%3A5000/%2", "/http%3a%2f%2f%5Bfoo%5D%3A5000/%252")]
    [InlineData("/http%3a%2f%2f[foo]%3A5000/%2F", "/http%3a%2f%2f%5Bfoo%5D%3A5000/%2F")]
    public void ToUriComponentEscapeCorrectly(string input, string expected)
    {
        var path = new PathString(input);

        Assert.Equal(expected, path.ToUriComponent());
    }

    [Fact]
    public void PathStringConvertsOnlyToAndFromString()
    {
        var converter = TypeDescriptor.GetConverter(typeof(PathString));
        PathString result = (PathString)converter.ConvertFromInvariantString("/foo")!;
        Assert.Equal("/foo", result.ToString());
        Assert.Equal("/foo", converter.ConvertTo(result, typeof(string)));
        Assert.True(converter.CanConvertFrom(typeof(string)));
        Assert.False(converter.CanConvertFrom(typeof(int)));
        Assert.False(converter.CanConvertFrom(typeof(bool)));
        Assert.True(converter.CanConvertTo(typeof(string)));
        Assert.False(converter.CanConvertTo(typeof(int)));
        Assert.False(converter.CanConvertTo(typeof(bool)));
    }

    [Fact]
    public void PathStringStaysEqualAfterAssignments()
    {
        PathString p1 = "/?";
        string s1 = p1;
        PathString p2 = s1;
        Assert.Equal(p1, p2);
    }

    [Theory]
    [InlineData("/a%2Fb")]
    [InlineData("/a%2F")]
    [InlineData("/%2fb")]
    [InlineData("/a%2Fb/c%2Fd/e")]
    public void StringFromUriComponentLeavesForwardSlashEscaped(string input)
    {
        var sut = PathString.FromUriComponent(input);
        Assert.Equal(input, sut.Value);
    }

    [Theory]
    [InlineData("/a%2Fb")]
    [InlineData("/a%2F")]
    [InlineData("/%2fb")]
    [InlineData("/a%2Fb/c%2Fd/e")]
    public void UriFromUriComponentLeavesForwardSlashEscaped(string input)
    {
        var uri = new Uri($"https://localhost:5001{input}");
        var sut = PathString.FromUriComponent(uri);
        Assert.Equal(input, sut.Value);
    }

    [Theory]
    [InlineData("/a%20b", "/a b")]
    [InlineData("/thisMustBeAVeryLongPath/SoLongThatItCouldActuallyBeLargerToTheStackAllocThresholdValue/PathsShorterToThisAllocateLessOnHeapByUsingStackAllocation/api/a%20b",
        "/thisMustBeAVeryLongPath/SoLongThatItCouldActuallyBeLargerToTheStackAllocThresholdValue/PathsShorterToThisAllocateLessOnHeapByUsingStackAllocation/api/a b")]
    public void StringFromUriComponentUnescapes(string input, string expected)
    {
        var sut = PathString.FromUriComponent(input);
        Assert.Equal(expected, sut.Value);
    }

    [Theory]
    [InlineData("/a%20b", "/a b")]
    [InlineData("/thisMustBeAVeryLongPath/SoLongThatItCouldActuallyBeLargerToTheStackAllocThresholdValue/PathsShorterToThisAllocateLessOnHeapByUsingStackAllocation/api/a%20b",
"/thisMustBeAVeryLongPath/SoLongThatItCouldActuallyBeLargerToTheStackAllocThresholdValue/PathsShorterToThisAllocateLessOnHeapByUsingStackAllocation/api/a b")]
    public void UriFromUriComponentUnescapes(string input, string expected)
    {
        var uri = new Uri($"https://localhost:5001{input}");
        var sut = PathString.FromUriComponent(uri);
        Assert.Equal(expected, sut.Value);
    }

    [Theory]
    [InlineData("/a%2Fb")]
    [InlineData("/a%2F")]
    [InlineData("/%2fb")]
    [InlineData("/%2Fb%20c")]
    [InlineData("/a%2Fb%20c")]
    [InlineData("/a%20b")]
    [InlineData("/a%2Fb/c%2Fd/e%20f")]
    [InlineData("/%E4%BD%A0%E5%A5%BD")]
    public void FromUriComponentToUriComponent(string input)
    {
        var sut = PathString.FromUriComponent(input);
        Assert.Equal(input, sut.ToUriComponent());
    }

    [Theory]
    [MemberData(nameof(CharsToUnescape))]
    [InlineData("/%E4%BD%A0%E5%A5%BD", "/你好")]
    public void FromUriComponentUnescapesAllExceptForwardSlash(string input, string expected)
    {
        var sut = PathString.FromUriComponent(input);
        Assert.Equal(expected, sut.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void ExercisingStringFromUriComponentOnStackAllocLimit(int offset)
    {
        var path = "/";
        var testString = new string('a', PathString.StackAllocThreshold + offset - path.Length);
        var sut = PathString.FromUriComponent(path + testString);
        Assert.Equal(PathString.StackAllocThreshold + offset, sut.Value!.Length);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void ExercisingUriFromUriComponentOnStackAllocLimit(int offset)
    {
        var localhost = "https://localhost:5001/";
        var testString = new string('a', PathString.StackAllocThreshold + offset);
        var sut = PathString.FromUriComponent(new Uri(localhost + testString));
        Assert.Equal(PathString.StackAllocThreshold + offset + 1, sut.Value!.Length);
    }

    public static IEnumerable<object[]> CharsToUnescape
    {
        get
        {
            foreach (var item in Enumerable.Range(1, 127))
            {
                // %2F is '/' not escaped for paths
                if (item != 0x2f)
                {
                    var hexEscapedValue = "%" + item.ToString("x2", CultureInfo.InvariantCulture);
                    var expected = Uri.UnescapeDataString(hexEscapedValue);
                    yield return new object[] { "/a" + hexEscapedValue, "/a" + expected };
                }
            }
        }
    }
}
