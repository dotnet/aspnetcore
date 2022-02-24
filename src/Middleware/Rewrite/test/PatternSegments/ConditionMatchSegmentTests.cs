// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class ConditionMatchSegmentTests
{

    [Theory]
    [InlineData(1, "foo")]
    [InlineData(2, "bar")]
    [InlineData(3, "baz")]
    public void ConditionMatch_AssertBackreferencesObtainsCorrectValue(int index, string expected)
    {
        // Arrange
        var condMatch = CreateTestMatch();
        var segment = new ConditionMatchSegment(index);

        // Act
        var results = segment.Evaluate(null, null, condMatch.BackReferences);

        // Assert
        Assert.Equal(expected, results);
    }

    private static MatchResults CreateTestMatch()
    {
        var match = Regex.Match("foo/bar/baz", "(.*)/(.*)/(.*)");
        return new MatchResults(match.Success, new BackReferenceCollection(match.Groups));
    }
}
