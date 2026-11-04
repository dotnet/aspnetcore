// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class LiteralSegmentTests
{
    [Fact]
    public void LiteralSegment_AssertSegmentIsCorrect()
    {
        // Arrange
        var segement = new LiteralSegment("foo");

        // Act
        var results = segement.Evaluate(null, null, null);

        // Assert
        Assert.Equal("foo", results);
    }
}
