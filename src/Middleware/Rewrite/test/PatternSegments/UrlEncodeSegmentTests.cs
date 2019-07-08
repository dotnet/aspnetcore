// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
    public class UrlEncodeSegmentTests
    {
        [Theory]
        [InlineData(" ", "%20")]
        [InlineData("x&y", "x%26y")]
        [InlineData("hey", "hey")]
        public void ToLower_AssertLowerCaseWorksAppropriately(string input, string expected)
        {
            // Arrange
            var pattern = new Pattern(new List<PatternSegment>());
            pattern.PatternSegments.Add(new LiteralSegment(input));
            var segement = new UrlEncodeSegment(pattern);
            var context = new RewriteContext();

            // Act
            var results = segement.Evaluate(context, null, null);

            // Assert
            Assert.Equal(expected, results);
        }
    }
}
