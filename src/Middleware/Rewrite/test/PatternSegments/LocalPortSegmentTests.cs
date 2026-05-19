// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class LocalPortSegmentTests
{
    [Fact]
    public void LocalPortSegment_AssertSegmentIsCorrect()
    {
        // Arrange
        var segement = new LocalPortSegment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Connection.LocalPort = 800;
        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Equal("800", results);
    }
}
