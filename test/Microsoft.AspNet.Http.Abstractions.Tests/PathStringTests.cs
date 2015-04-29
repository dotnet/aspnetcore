// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Http
{
    public class PathStringTests
    {
        [Fact]
        public void CtorThrows_IfPathDoesNotHaveLeadingSlash()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => new PathString("hello"), "value", "");
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        public void AddPathString_HandlesNullAndEmptyStrings(string appString, string concatString)
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
        public void AddPathString_HandlesLeadingAndTrailingSlashes(string appString, string concatString, string expected)
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
    }
}
