// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
    public class IsHttpsModSegmentTests
    {
        [Theory]
        [InlineData("http", "off")]
        [InlineData("https", "on")]
        public void IsHttps_AssertCorrectBehaviorWhenProvidedHttpContext(string input, string expected)
        {
            // Arrange
            var segement = new IsHttpsModSegment();
            var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
            context.HttpContext.Request.Scheme = input;

            // Act
            var results = segement.Evaluate(context, null, null);

            // Assert
            Assert.Equal(expected, results);
        }
    }
}
