// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
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
}
