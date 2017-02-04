// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal.PatternSegments;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
    public class GlobalRuleUrlSegmentTests
    {
        [Theory]
        [InlineData("http", "localhost", 80, null, null, "http://localhost:80/")]
        [InlineData("http", "localhost", 80, "/foo/bar", null, "http://localhost:80/foo/bar")]
        [InlineData("http", "localhost", 80, "/foo bar", null, "http://localhost:80/foo%20bar")]
        [InlineData("http", "localhost", 81, "/foo/bar", null, "http://localhost:81/foo/bar")]
        [InlineData("http", "localhost", 80, null, "?foo=bar", "http://localhost:80/?foo=bar")]
        [InlineData("https", "localhost", 443, "/foo/bar", null, "https://localhost:443/foo/bar")]
        public void AssertSegmentIsCorrect(string scheme, string host, int port, string path, string queryString, string expectedResult)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = scheme;
            httpContext.Request.Host = new HostString(host, port);

            if (!string.IsNullOrEmpty(path))
            {
                httpContext.Request.Path = new PathString(path);
            }

            if (!string.IsNullOrEmpty(queryString))
            {
                httpContext.Request.QueryString = new QueryString(queryString);
            }

            var context = new RewriteContext { HttpContext = httpContext };
            context.HttpContext = httpContext;

            // Act
            var segment = new GlobalRuleUrlSegment();
            var results = segment.Evaluate(context, null, null);

            // Assert
            Assert.Equal(expectedResult, results);
        }
    }
}