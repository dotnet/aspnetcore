// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    public class RuleRegexParserTest
    {
        [Fact]
        public void RuleRegexParser_ShouldThrowOnNull()
        {
            Assert.Throws<FormatException>(() => new RuleRegexParser().ParseRuleRegex(null));
        }

        [Fact]
        public void RuleRegexParser_ShouldThrowOnEmpty()
        {
            Assert.Throws<FormatException>(() => new RuleRegexParser().ParseRuleRegex(string.Empty));
        }

        [Fact]
        public void RuleRegexParser_RegularRegexExpression()
        {
            var results = new RuleRegexParser().ParseRuleRegex("(.*)");
            Assert.False(results.Invert);
            Assert.Equal("(.*)", results.Operand);
        }
    }
}
