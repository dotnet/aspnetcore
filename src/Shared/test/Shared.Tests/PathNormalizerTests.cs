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
}
