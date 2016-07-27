// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    public class RuleRegexParserTest
    {
        [Fact]
        public void RuleRegexParser_ShouldThrowOnNull()
        {
            Assert.Throws<FormatException>(() => RuleRegexParser.ParseRuleRegex(null));
        }

        [Fact]
        public void RuleRegexParser_ShouldThrowOnEmpty()
        {
            Assert.Throws<FormatException>(() => RuleRegexParser.ParseRuleRegex(string.Empty));
        }

        [Fact]
        public void RuleRegexParser_RegularRegexExpression()
        {
            var results = RuleRegexParser.ParseRuleRegex("(.*)");
            Assert.False(results.Invert);
            Assert.Equal(results.Operand, "(.*)");
        }
    }
}
