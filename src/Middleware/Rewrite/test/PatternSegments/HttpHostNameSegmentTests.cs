// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
    public class HttpHostNameSegmentTests
    {
        [Fact]
        public void HttpHostNameSegment_AssertGettingHostName()
        {
            // Arrange
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };

            context.HttpContext.Request.Headers[HeaderNames.Host] = "example.com:443";
            var segment = new HttpHostNameSegment();

            // Act
            var results = segment.Evaluate(context, null, null);

            // Assert
            Assert.Equal("example.com", results);
        }
    }
}
