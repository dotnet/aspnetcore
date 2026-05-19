// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class RequestFilenameSegmentTests
{
    [Fact]
    public void RequestFilename_AssertSegmentIsCorrect()
    {
        // Arrange
        var segement = new RequestFileNameSegment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Request.Path = new PathString("/foo/bar");
        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Equal("/foo/bar", results);
    }
}
