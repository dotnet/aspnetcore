// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class ServerNameSegmentTests
{
    [Theory]
    [InlineData("foobar", 80, "foobar")]
    [InlineData("foobar", 443, "foobar")]
    [InlineData("foobar", 8080, "foobar")]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334", 8080, "[2001:0db8:85a3:0000:0000:8a2e:0370:7334]")]
    [InlineData("127.0.0.1", 8080, "127.0.0.1")]
    public void AssertServerNameIsCorrect(string host, int port, string expectedResult)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host, port);
        httpContext.Request.Path = new PathString("/foo/bar");

        var context = new RewriteContext { HttpContext = httpContext };
        context.HttpContext = httpContext;

        // Act
        var segment = new ServerNameSegment();
        var results = segment.Evaluate(context, null, null);

        // Assert
        Assert.Equal(expectedResult, results);
    }
}
