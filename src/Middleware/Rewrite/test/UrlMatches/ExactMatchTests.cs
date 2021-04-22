// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.UrlActions;
using Microsoft.AspNetCore.Rewrite.UrlMatches;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlMatches
{
    public class ExactMatchTests
    {
        [Theory]
        [InlineDataAttribute(true,"string",false,"string",true)]
        [InlineDataAttribute(true, "string", true, "string", false)]
        [InlineDataAttribute(false, "STRING", false, "string",false)]
        [InlineDataAttribute(false, "STRING", true, "string", true)]
        public void ExactMatch_Case_Sensitivity_Negate_Tests(bool ignoreCase, string inputString, bool negate, string pattern, bool expectedResult)
        {
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            var Match = new ExactMatch(ignoreCase, inputString, negate);
            var matchResults = Match.Evaluate(pattern, context);
            Assert.Equal(expectedResult, matchResults.Success);
        }
    }
}
