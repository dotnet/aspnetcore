using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Rewrite.ModRewrite;
using Microsoft.AspNetCore.Rewrite.Operands;
using Microsoft.AspNetCore.Rewrite.RuleAbstraction;
using Xunit;
namespace Microsoft.AspNetCore.Rewrite.Tests.RuleAbstraction
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
