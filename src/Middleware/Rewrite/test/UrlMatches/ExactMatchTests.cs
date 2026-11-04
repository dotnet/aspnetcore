// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlMatches;

public class ExactMatchTests
{
    [Theory]
    [InlineDataAttribute(true, "string", false, "string", true)]
    [InlineDataAttribute(true, "string", true, "string", false)]
    [InlineDataAttribute(false, "STRING", false, "string", false)]
    [InlineDataAttribute(false, "STRING", true, "string", true)]
    public void ExactMatch_Case_Sensitivity_Negate_Tests(bool ignoreCase, string inputString, bool negate, string pattern, bool expectedResult)
    {
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        var Match = new ExactMatch(ignoreCase, inputString, negate);
        var matchResults = Match.Evaluate(pattern, context);
        Assert.Equal(expectedResult, matchResults.Success);
    }
}
