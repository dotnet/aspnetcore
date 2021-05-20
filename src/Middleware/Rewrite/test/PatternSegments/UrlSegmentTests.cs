// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
    public class UrlSegmentTests
    {
        [Theory]
        [InlineData("http", "localhost", 80, null, (int)UriMatchPart.Path, "")]
        [InlineData("http", "localhost", 80, "", (int)UriMatchPart.Path, "")]
        [InlineData("http", "localhost", 80, "/foo/bar", (int)UriMatchPart.Path, "/foo/bar")]
        [InlineData("http", "localhost", 80, "/foo:bar", (int)UriMatchPart.Path, "/foo:bar")]
        [InlineData("http", "localhost", 80, "/foo bar", (int)UriMatchPart.Path, "/foo%20bar")]
        [InlineData("http", "localhost", 80, null, (int)UriMatchPart.Full, "http://localhost:80/")]
        [InlineData("http", "localhost", 80, "", (int)UriMatchPart.Full, "http://localhost:80/")]
        [InlineData("http", "localhost", 80, "/foo:bar", (int)UriMatchPart.Full, "http://localhost:80/foo:bar")]
        [InlineData("http", "localhost", 80, "/foo bar", (int)UriMatchPart.Full, "http://localhost:80/foo%20bar")]
        [InlineData("http", "localhost", 80, "/foo/bar", (int)UriMatchPart.Full, "http://localhost:80/foo/bar")]
        [InlineData("http", "localhost", 81, "/foo/bar", (int)UriMatchPart.Full, "http://localhost:81/foo/bar")]
        [InlineData("https", "localhost", 443, "/foo/bar", (int)UriMatchPart.Full, "https://localhost:443/foo/bar")]
        public void AssertSegmentIsCorrect(string scheme, string host, int port, string path, int uriMatchPart, string expectedResult)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = scheme;
            httpContext.Request.Host = new HostString(host, port);
            httpContext.Request.Path = new PathString(path);

            var context = new RewriteContext { HttpContext = httpContext };
            context.HttpContext = httpContext;

            // Act
            var segment = new UrlSegment((UriMatchPart)uriMatchPart);
            var results = segment.Evaluate(context, null, null);

            // Assert
            Assert.Equal(expectedResult, results);
        }
    }
}