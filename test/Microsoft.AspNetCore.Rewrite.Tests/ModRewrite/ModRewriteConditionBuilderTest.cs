// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite;
using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite.Operands;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    // This file tests an input of a list of tokens and verifies that the appropriate condition is obtained
    public class ModRewriteConditionBuilderTest
    {
        [Fact]
        public void ConditionBuilder_PassInNoFlagsFlagsEmpty()
        {
            var conditionString = "RewriteCond /$1 /hello";
            var builder = new ConditionBuilder(conditionString);
            var results = builder.Build();

            //var expected = new Condition(
            //    new Pattern(
            //        new List<PatternSegment>() {
            //            new PatternSegment("/", SegmentType.Literal),
            //            new PatternSegment("1", SegmentType.RuleParameter)
            //        }), 
            //    new ConditionExpression { Operand = new RegexOperand {Regex = new Regex("/hello") } }, 
            //    new ConditionFlags());
            var expected = (new ConditionBuilder("/$1", "/hello")).Build();

            Assert.True(results.Flags.FlagDictionary.Count == 0);
            Assert.True(results.Flags.FlagDictionary.Count == expected.Flags.FlagDictionary.Count);
            Assert.True((results.ConditionExpression.Operand is RegexOperand) 
                && (expected.ConditionExpression.Operand is RegexOperand));
        }

        [Fact]
        public void ConditionBuilder_PassInFlagsFlagsExist()
        {
            var conditionString = "RewriteCond /$1 /hello [NC]";
            var builder = new ConditionBuilder(conditionString);
            var results = builder.Build();
            var expected = (new ConditionBuilder("/$1", "/hello", "[NC]")).Build();

            Assert.True(results.Flags.FlagDictionary.Count == 1);
            Assert.True(results.Flags.FlagDictionary.Count == expected.Flags.FlagDictionary.Count);
            Assert.True((results.ConditionExpression.Operand is RegexOperand)
                && (expected.ConditionExpression.Operand is RegexOperand));
        }
    }
}
