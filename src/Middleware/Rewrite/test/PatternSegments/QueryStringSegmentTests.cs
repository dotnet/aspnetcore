// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class QueryStringSegmentTests
{
    [Fact]
    public void QueryString_AssertSegmentIsCorrect()
    {
        var segement = new QueryStringSegment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Request.QueryString = new QueryString("?hey=1");

        var results = segement.Evaluate(context, null, null);

        Assert.Equal("hey=1", results);
    }
}
