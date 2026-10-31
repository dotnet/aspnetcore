// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class RemotePortSegmentTests
{
    [Fact]
    public void RemotePort_AssertSegmentIsCorrect()
    {
        // Arrange
        var segement = new RemotePortSegment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Connection.RemotePort = 800;
        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Equal("800", results);
    }
}
