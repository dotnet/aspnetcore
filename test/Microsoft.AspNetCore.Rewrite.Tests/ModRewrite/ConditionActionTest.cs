// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    public class ConditionActionTest
    {
        [Theory]
        [InlineData(">hey", OperationType.Greater, "hey", ConditionType.StringComp)]
        [InlineData("<hey", OperationType.Less, "hey", ConditionType.StringComp)]
        [InlineData(">=hey", OperationType.GreaterEqual, "hey", ConditionType.StringComp)]
        [InlineData("<=hey", OperationType.LessEqual, "hey", ConditionType.StringComp)]
        [InlineData("=hey", OperationType.Equal, "hey", ConditionType.StringComp)]
        public void ConditionParser_CheckStringComp(string condition, OperationType operation, string variable, ConditionType conditionType)
        {
            var results = ConditionPatternParser.ParseActionCondition(condition);

            var expected = new ParsedModRewriteExpression { Operation = operation, Type = conditionType, Operand = variable, Invert = false };
            Assert.True(CompareConditions(results, expected));
        }

        [Fact]
        public void ConditionParser_CheckRegexEqual()
        {
            var condition = @"(.*)";
            var results = ConditionPatternParser.ParseActionCondition(condition);

            var expected = new ParsedModRewriteExpression { Type = ConditionType.Regex, Operand = "(.*)",  Invert = false };
            Assert.True(CompareConditions(results, expected));
        }

        [Theory]
        [InlineData("-d", OperationType.Directory, ConditionType.PropertyTest)]
        [InlineData("-f", OperationType.RegularFile, ConditionType.PropertyTest)]
        [InlineData("-F", OperationType.ExistingFile, ConditionType.PropertyTest)]
        [InlineData("-h", OperationType.SymbolicLink, ConditionType.PropertyTest)]
        [InlineData("-L", OperationType.SymbolicLink, ConditionType.PropertyTest)]
        [InlineData("-l", OperationType.SymbolicLink, ConditionType.PropertyTest)]
        [InlineData("-s", OperationType.Size, ConditionType.PropertyTest)]
        [InlineData("-U", OperationType.ExistingUrl, ConditionType.PropertyTest)]
        [InlineData("-x", OperationType.Executable, ConditionType.PropertyTest)]
        public void ConditionParser_CheckFileOperations(string condition, OperationType operation, ConditionType cond)
        {
            var results = ConditionPatternParser.ParseActionCondition(condition);

            var expected = new ParsedModRewriteExpression { Type = cond, Operation = operation , Invert = false };
            Assert.True(CompareConditions(results, expected));
        }

        [Theory]
        [InlineData("!-d", OperationType.Directory, ConditionType.PropertyTest)]
        [InlineData("!-f", OperationType.RegularFile, ConditionType.PropertyTest)]
        [InlineData("!-F", OperationType.ExistingFile, ConditionType.PropertyTest)]
        [InlineData("!-h", OperationType.SymbolicLink, ConditionType.PropertyTest)]
        [InlineData("!-L", OperationType.SymbolicLink, ConditionType.PropertyTest)]
        [InlineData("!-l", OperationType.SymbolicLink, ConditionType.PropertyTest)]
        [InlineData("!-s", OperationType.Size, ConditionType.PropertyTest)]
        [InlineData("!-U", OperationType.ExistingUrl, ConditionType.PropertyTest)]
        [InlineData("!-x", OperationType.Executable, ConditionType.PropertyTest)]
        public void ConditionParser_CheckFileOperationsInverted(string condition, OperationType operation, ConditionType cond)
        {
            var results = ConditionPatternParser.ParseActionCondition(condition);

            var expected = new ParsedModRewriteExpression { Type = cond, Operation = operation, Invert = true };
            Assert.True(CompareConditions(results, expected));
        }

        [Theory]
        [InlineData("-gt1", OperationType.Greater, "1", ConditionType.IntComp)]
        [InlineData("-lt1", OperationType.Less, "1", ConditionType.IntComp)]
        [InlineData("-ge1", OperationType.GreaterEqual, "1", ConditionType.IntComp)]
        [InlineData("-le1", OperationType.LessEqual, "1", ConditionType.IntComp)]
        [InlineData("-eq1", OperationType.Equal, "1", ConditionType.IntComp)]
        [InlineData("-ne1", OperationType.NotEqual, "1", ConditionType.IntComp)]
        public void ConditionParser_CheckIntComp(string condition, OperationType operation, string variable, ConditionType cond)
        {
            var results = ConditionPatternParser.ParseActionCondition(condition);

            var expected = new ParsedModRewriteExpression { Type = cond, Operation = operation, Invert = false, Operand = variable };
            Assert.True(CompareConditions(results, expected));
        }

        // TODO negative tests
        private bool CompareConditions(ParsedModRewriteExpression i1, ParsedModRewriteExpression i2)
        {
            if (i1.Operation != i2.Operation ||
                i1.Type != i2.Type ||
                i1.Operand != i2.Operand ||
                i1.Invert != i2.Invert)
            {
                return false;
            }
            return true;
        }
    }
}
