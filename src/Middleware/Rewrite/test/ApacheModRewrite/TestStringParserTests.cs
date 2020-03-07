// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    public class TestStringParserTests
    {
        [Fact]
        public void ConditionParser_SingleServerVariable()
        {
            var serverVar = "%{HTTPS}";

            var result = new TestStringParser().Parse(serverVar);

            var list = new List<PatternSegment>();
            list.Add(new IsHttpsModSegment());
            var expected = new Pattern(list);
            AssertPatternsEqual(expected, result);
        }

        [Fact]
        public void ConditionParser_MultipleServerVariables()
        {
            var serverVar = "%{HTTPS}%{REQUEST_URI}";
            var result = new TestStringParser().Parse(serverVar);

            var list = new List<PatternSegment>();
            list.Add(new IsHttpsModSegment());
            list.Add(new UrlSegment());
            var expected = new Pattern(list);
            AssertPatternsEqual(expected, result);
        }

        [Fact]
        public void ConditionParser_ParseLiteral()
        {
            var serverVar = "Hello!";
            var result = new TestStringParser().Parse(serverVar);

            var list = new List<PatternSegment>();
            list.Add(new LiteralSegment(serverVar));
            var expected = new Pattern(list);
            AssertPatternsEqual(expected, result);
        }

        [Fact]
        public void ConditionParser_ParseConditionParameters()
        {
            var serverVar = "%1";
            var result = new TestStringParser().Parse(serverVar);

            var list = new List<PatternSegment>();
            list.Add(new ConditionMatchSegment(1));
            var expected = new Pattern(list);
            AssertPatternsEqual(expected, result);
        }

        [Fact]
        public void ConditionParser_ParseMultipleConditionParameters()
        {
            var serverVar = "%1%2";
            var result = new TestStringParser().Parse(serverVar);

            var list = new List<PatternSegment>();
            list.Add(new ConditionMatchSegment(1));
            list.Add(new ConditionMatchSegment(2));
            var expected = new Pattern(list);
            AssertPatternsEqual(expected, result);
        }

        [Fact]
        public void ConditionParser_ParseRuleVariable()
        {
            var serverVar = "$1";
            var result = new TestStringParser().Parse(serverVar);

            var list = new List<PatternSegment>();
            list.Add(new RuleMatchSegment(1));
            var expected = new Pattern(list);
            AssertPatternsEqual(expected, result);
        }
        [Fact]
        public void ConditionParser_ParseMultipleRuleVariables()
        {
            var serverVar = "$1$2";
            var result = new TestStringParser().Parse(serverVar);

            var list = new List<PatternSegment>();
            list.Add(new RuleMatchSegment(1));
            list.Add(new RuleMatchSegment(2));
            var expected = new Pattern(list);
            AssertPatternsEqual(expected, result);
        }

        [Fact]
        public void ConditionParser_ParserComplexRequest()
        {
            var serverVar = "%{HTTPS}/$1";
            var result = new TestStringParser().Parse(serverVar);

            var list = new List<PatternSegment>();
            list.Add(new IsHttpsModSegment());
            list.Add(new LiteralSegment("/"));
            list.Add(new RuleMatchSegment(1));
            var expected = new Pattern(list);
            AssertPatternsEqual(expected, result);
        }

        [Theory]
        [InlineData(@"%}", "Cannot parse '%}' to integer at string index: '1'")] // no } at end
        [InlineData(@"%{", "Missing close brace for parameter at string index: '2'")] // no closing }
        [InlineData(@"%a", "Cannot parse '%a' to integer at string index: '1'")] // invalid character after %
        [InlineData(@"$a", "Cannot parse '$a' to integer at string index: '1'")] // invalid character after $
        [InlineData(@"%{asdf", "Missing close brace for parameter at string index: '6'")] // no closing } with characters
        public void ConditionParser_InvalidInput(string testString, string expected)
        {
            var ex = Assert.Throws<FormatException>(() => new TestStringParser().Parse(testString));
            Assert.Equal(expected, ex.Message);
        }

        private void AssertPatternsEqual(Pattern p1, Pattern p2)
        {
            Assert.Equal(p1.PatternSegments.Count, p2.PatternSegments.Count);

            for (int i = 0; i < p1.PatternSegments.Count; i++)
            {
                var s1 = p1.PatternSegments[i];
                var s2 = p2.PatternSegments[i];

                Assert.Equal(s1.GetType(), s2.GetType());
            }
        }
    }
}
