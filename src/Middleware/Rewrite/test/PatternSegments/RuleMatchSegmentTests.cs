// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments
{
    public class RuleMatchSegmentTests
    {
        [Theory]
        [InlineData(1, "foo")]
        [InlineData(2, "bar")]
        [InlineData(3, "baz")]
        public void RuleMatch_AssertBackreferencesObtainsCorrectValue(int index, string expected)
        {
            // Arrange
            var ruleMatch = CreateTestMatch();
            var segment = new RuleMatchSegment(index);

            // Act
            var results = segment.Evaluate(null, ruleMatch.BackReferences, null);

            // Assert
            Assert.Equal(expected, results);
        }

        private static MatchResults CreateTestMatch()
        {
            var match = Regex.Match("foo/bar/baz", "(.*)/(.*)/(.*)");
            return new MatchResults { BackReferences = new BackReferenceCollection(match.Groups), Success = match.Success };
        }
    }
}
