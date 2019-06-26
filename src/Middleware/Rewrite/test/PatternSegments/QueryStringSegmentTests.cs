// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
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
}
