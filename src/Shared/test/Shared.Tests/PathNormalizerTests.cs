// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.HttpSys.Internal
{
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
        public void RemovesDotSegments(string input, string expected)
        {
            var data = Encoding.ASCII.GetBytes(input);
            var length = PathNormalizer.RemoveDotSegments(new Span<byte>(data));
            Assert.True(length >= 1);
            Assert.Equal(expected, Encoding.ASCII.GetString(data, 0, length));
        }
    }
}
