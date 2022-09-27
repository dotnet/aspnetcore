// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class IsHttpsModSegmentTests
{
    [Theory]
    [InlineData("http", "off")]
    [InlineData("https", "on")]
    public void IsHttps_AssertCorrectBehaviorWhenProvidedHttpContext(string input, string expected)
    {
        // Arrange
        var segement = new IsHttpsModSegment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Request.Scheme = input;

        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Equal(expected, results);
    }
}
