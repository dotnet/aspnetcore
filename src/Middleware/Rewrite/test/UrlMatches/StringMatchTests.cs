// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlMatches;

public class StringMatchTests
{
    [Theory]
    [InlineData("hi", (int)StringOperationType.Equal, true, "hi", true)]
    [InlineData("a", (int)StringOperationType.Greater, true, "b", true)]
    [InlineData("a", (int)StringOperationType.GreaterEqual, true, "b", true)]
    [InlineData("b", (int)StringOperationType.Less, true, "a", true)]
    [InlineData("b", (int)StringOperationType.LessEqual, true, "a", true)]
    [InlineData("", (int)StringOperationType.Equal, true, "", true)]
    [InlineData(null, (int)StringOperationType.Equal, true, null, true)]
    public void StringMatch_Evaluation_Check_Cases(string value, int operation, bool ignoreCase, string input, bool expectedResult)
    {
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        var stringMatch = new StringMatch(value, (StringOperationType)operation, ignoreCase);
        var matchResult = stringMatch.Evaluate(input, context);
        Assert.Equal(expectedResult, matchResult.Success);
    }
}
