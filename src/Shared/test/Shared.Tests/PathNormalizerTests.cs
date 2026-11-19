// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Xunit;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Internal.Tests;

public class PathNormalizerTests
{
    [Theory]
    [InlineData("/a", "/a")]
    [InlineData("/a/", "/a/")]
    [InlineData("/a/b", "/a/b")]
    [InlineData("/a/b/", "/a/b/")]
    [InlineData("/./a", "/a")]
    [InlineData("/././a", "/a")]
    [InlineData("/../a", "/a")]
    [InlineData("/../../a", "/a")]
    [InlineData("/a/./b", "/a/b")]
    [InlineData("/a/../b", "/b")]
    [InlineData("/a/./", "/a/")]
    [InlineData("/a/.", "/a/")]
    [InlineData("/a/../", "/")]
    [InlineData("/a/..", "/")]
    [InlineData("/a/../b/../", "/")]
    [InlineData("/a/../b/..", "/")]
    [InlineData("/a/../../b", "/b")]
    [InlineData("/a/../../b/", "/b/")]
    [InlineData("/a/.././../b", "/b")]
    [InlineData("/a/.././../b/", "/b/")]
    [InlineData("/a/b/c/./../../d", "/a/d")]
    [InlineData("/./a/b/c/./../../d", "/a/d")]
    [InlineData("/../a/b/c/./../../d", "/a/d")]
    [InlineData("/./../a/b/c/./../../d", "/a/d")]
    [InlineData("/.././a/b/c/./../../d", "/a/d")]
    [InlineData("/.a", "/.a")]
    [InlineData("/..a", "/..a")]
    [InlineData("/...", "/...")]
    [InlineData("/a/.../b", "/a/.../b")]
    [InlineData("/a/../.../../b", "/b")]
    [InlineData("/a/.b", "/a/.b")]
    [InlineData("/a/..b", "/a/..b")]
    [InlineData("/a/b.", "/a/b.")]
    [InlineData("/a/b..", "/a/b..")]
    [InlineData("/longlong/../short", "/short")]
    [InlineData("/short/../longlong", "/longlong")]
    [InlineData("/longlong/../short/..", "/")]
    [InlineData("/short/../longlong/..", "/")]
    [InlineData("/longlong/../short/../", "/")]
    [InlineData("/short/../longlong/../", "/")]
    [InlineData("/", "/")]
    [InlineData("/no/segments", "/no/segments")]
    [InlineData("/no/segments/", "/no/segments/")]
    [InlineData("/././", "/")]
    [InlineData("/./.", "/")]
    [InlineData("/../..", "/")]
    [InlineData("/../../", "/")]
    [InlineData("/../.", "/")]
    [InlineData("/./..", "/")]
    [InlineData("/.././", "/")]
    [InlineData("/./../", "/")]
    [InlineData("/..", "/")]
    [InlineData("/.", "/")]
    [InlineData("/a/abc/../abc/../b", "/a/b")]
    [InlineData("/a/abc/.a", "/a/abc/.a")]
    [InlineData("/a/abc/..a", "/a/abc/..a")]
    [InlineData("/a/.b/c", "/a/.b/c")]
    [InlineData("/a/.b/../c", "/a/c")]
    [InlineData("/a/../.b/./c", "/.b/c")]
    [InlineData("/a/.b/./c", "/a/.b/c")]
    [InlineData("/a/./.b/./c", "/a/.b/c")]
    [InlineData("/a/..b/c", "/a/..b/c")]
    [InlineData("/a/..b/../c", "/a/c")]
    [InlineData("/a/../..b/./c", "/..b/c")]
    [InlineData("/a/..b/./c", "/a/..b/c")]
    [InlineData("/a/./..b/./c", "/a/..b/c")]
    public void RemovesDotSegments(string input, string expected)
    {
        var data = Encoding.ASCII.GetBytes(input);
        var length = PathNormalizer.RemoveDotSegments(new Span<byte>(data));
        Assert.True(length >= 1);
        Assert.Equal(expected, Encoding.ASCII.GetString(data, 0, length));
    }

    [Theory]
    [InlineData("/a", "/a", false)]
    [InlineData("/a/b", "/a/b", false)]
    [InlineData(@"/a\b", "/a/b", true)]
    [InlineData(@"\", "/", true)]
    [InlineData(@"/a\b\c", "/a/b/c", true)]
    [InlineData(@"/a\\b", "/a//b", true)]
    public void NormalizesBackslashes(string input, string expected, bool expectedNormalized)
    {
        var data = Encoding.ASCII.GetBytes(input);
        var normalized = PathNormalizer.NormalizeBackslashes(new Span<byte>(data));
        Assert.Equal(expectedNormalized, normalized);
        Assert.Equal(expected, Encoding.ASCII.GetString(data));
    }

    [Theory]
    [InlineData(@"/a\b", "/a/b")]
    [InlineData(@"/a\..\b", "/b")]
    [InlineData(@"/secure\..\public", "/public")]
    [InlineData(@"/a/..\b", "/b")]
    [InlineData(@"/a\../b", "/b")]
    public void NormalizesBackslashesBeforeRemovingDotSegments(string input, string expected)
    {
        var data = Encoding.ASCII.GetBytes(input);
        var span = new Span<byte>(data);
        PathNormalizer.NormalizeBackslashes(span);
        var length = PathNormalizer.RemoveDotSegments(span);
        Assert.Equal(expected, Encoding.ASCII.GetString(data, 0, length));
    }
}
