// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal.PatternSegments;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
    public class QueryStringSegmentTests
    {
        [Fact]
        public void QueryString_AssertSegmentIsCorrect()
        {
            // Arrange
            var segement = new QueryStringSegment();
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            context.HttpContext.Request.QueryString = new QueryString("?hey=1");

            // Act
            var results = segement.Evaluate(context, null, null);

            // Assert
            Assert.Equal("?hey=1", results);
        }
    }
}
