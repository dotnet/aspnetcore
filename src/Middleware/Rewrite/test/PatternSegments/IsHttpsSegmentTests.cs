// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class IsHttpsSegmentTests
{
    [Theory]
    [InlineData("http", "OFF")]
    [InlineData("https", "ON")]
    public void IsHttps_AssertCorrectBehaviorWhenProvidedHttpContext(string input, string expected)
    {
        // Arrange
        var segement = new IsHttpsUrlSegment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Request.Scheme = input;

        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Equal(expected, results);
    }
}
