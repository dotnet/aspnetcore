// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
    public class HeaderSegmentTests
    {
        [Fact]
        public void HeaderSegment_AssertGettingWithHeaderReturnsCorrectValue()
        {
            // Arrange
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };

            context.HttpContext.Request.Headers[HeaderNames.Location] = "foo";
            var segment = new HeaderSegment(HeaderNames.Location);

            // Act
            var results = segment.Evaluate(context, null, null);

            // Assert
            Assert.Equal("foo", results);
        }

        [Fact]
        public void HeaderSegment_AssertGettingANonExistantHeaderReturnsNull()
        {
            // Arrange
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            var segment = new HeaderSegment(HeaderNames.Location);

            // Act
            var results = segment.Evaluate(context, null, null);

            // Assert
            Assert.Null(results);
        }
    }
}
