// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class UrlDecodeSegmentTests
{
    [Theory]
    [InlineData("%20", " ")]
    [InlineData("x%26y", "x&y")]
    [InlineData("hey", "hey")]
    public void FromLower_AssertLowerCaseWorksAppropriately(string input, string expected)
    {
        // Arrange
        var pattern = new Pattern(new List<PatternSegment>());
        pattern.PatternSegments.Add(new LiteralSegment(input));
        var segement = new UrlDecodeSegment(pattern);
        var context = new RewriteContext();

        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Equal(expected, results);
    }
}
