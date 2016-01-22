// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            ExceptionAssert.ThrowsArgument(() => new PathString("hello"), "value", "The path in 'value' must start with '/'.");
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
        [InlineData("/test/path", "/TEST", true)]
        [InlineData("/test/path", "/TEST/pa", false)]
        [InlineData("/TEST/PATH", "/test", true)]
        [InlineData("/TEST/path", "/test/pa", false)]
        [InlineData("/test/PATH/path/TEST", "/TEST/path/PATH", true)]
        public void StartsWithSegmentsWithRemainder_DoesACaseInsensitiveMatch(string sourcePath, string testPath, bool expectedResult)
        {
            var source = new PathString(sourcePath);
            var test = new PathString(testPath);

            PathString remaining;
            var result = source.StartsWithSegments(test, out remaining);

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

            PathString remaining;
            var result = source.StartsWithSegments(test, comparison, out remaining);

            Assert.Equal(expectedResult, result);
        }
    }
}
