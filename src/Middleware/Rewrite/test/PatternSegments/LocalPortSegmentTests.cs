// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
    public class LocalPortSegmentTests
    {
        [Fact]
        public void LocalPortSegment_AssertSegmentIsCorrect()
        {
            // Arrange
            var segement = new LocalPortSegment();
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            context.HttpContext.Connection.LocalPort = 800;
            // Act
            var results = segement.Evaluate(context, null, null);

            // Assert
            Assert.Equal("800", results);
        }
    }
}
