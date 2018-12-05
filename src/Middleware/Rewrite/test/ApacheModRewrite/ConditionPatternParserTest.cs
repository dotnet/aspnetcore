// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    public class ConditionPatternParserTest
    {
        [Theory]
        [InlineData(">hey", OperationType.Greater, "hey", ConditionType.StringComp)]
        [InlineData("<hey", OperationType.Less, "hey", ConditionType.StringComp)]
        [InlineData(">=hey", OperationType.GreaterEqual, "hey", ConditionType.StringComp)]
        [InlineData("<=hey", OperationType.LessEqual, "hey", ConditionType.StringComp)]
        [InlineData("=hey", OperationType.Equal, "hey", ConditionType.StringComp)]
        public void ConditionPatternParser_CheckStringComp(string condition, OperationType operation, string variable, ConditionType conditionType)
        {
            var results = new ConditionPatternParser().ParseActionCondition(condition);

            var expected = new ParsedModRewriteInput { OperationType = operation, ConditionType = conditionType, Operand = variable, Invert = false };
            Assert.True(CompareConditions(expected, results));
        }

        [Fact]
        public void ConditionPatternParser_CheckRegexEqual()
        {
            var condition = @"(.*)";
            var results = new ConditionPatternParser().ParseActionCondition(condition);

            var expected = new ParsedModRewriteInput { ConditionType = ConditionType.Regex, Operand = "(.*)", Invert = false };
            Assert.True(CompareConditions(expected, results));
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
        public void ConditionPatternParser_CheckFileOperations(string condition, OperationType operation, ConditionType cond)
        {
            var results = new ConditionPatternParser().ParseActionCondition(condition);

            var expected = new ParsedModRewriteInput { ConditionType = cond, OperationType = operation, Invert = false };
            Assert.True(CompareConditions(expected, results));
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
        public void ConditionPatternParser_CheckFileOperationsInverted(string condition, OperationType operation, ConditionType cond)
        {
            var results = new ConditionPatternParser().ParseActionCondition(condition);

            var expected = new ParsedModRewriteInput { ConditionType = cond, OperationType = operation, Invert = true };
            Assert.True(CompareConditions(expected, results));
        }

        [Theory]
        [InlineData("-gt1", OperationType.Greater, "1", ConditionType.IntComp)]
        [InlineData("-lt1", OperationType.Less, "1", ConditionType.IntComp)]
        [InlineData("-ge1", OperationType.GreaterEqual, "1", ConditionType.IntComp)]
        [InlineData("-le1", OperationType.LessEqual, "1", ConditionType.IntComp)]
        [InlineData("-eq1", OperationType.Equal, "1", ConditionType.IntComp)]
        [InlineData("-ne1", OperationType.NotEqual, "1", ConditionType.IntComp)]
        public void ConditionPatternParser_CheckIntComp(string condition, OperationType operation, string variable, ConditionType cond)
        {
            var results = new ConditionPatternParser().ParseActionCondition(condition);

            var expected = new ParsedModRewriteInput { ConditionType = cond, OperationType = operation, Invert = false, Operand = variable };
            Assert.True(CompareConditions(expected, results));
        }

        [Theory]
        [InlineData("", "Unrecognized parameter type: '', terminated at string index: '0'")]
        [InlineData("!", "Unrecognized parameter type: '!', terminated at string index: '1'")]
        [InlineData(">", "Unrecognized parameter type: '>', terminated at string index: '1'")]
        [InlineData("<", "Unrecognized parameter type: '<', terminated at string index: '1'")]
        [InlineData("=", "Unrecognized parameter type: '=', terminated at string index: '1'")]
        [InlineData(">=", "Unrecognized parameter type: '>=', terminated at string index: '2'")]
        [InlineData("<=", "Unrecognized parameter type: '<=', terminated at string index: '2'")]
        [InlineData("-a", "Unrecognized parameter type: '-a', terminated at string index: '1'")]
        [InlineData("-gewow", "Unrecognized parameter type: '-gewow', terminated at string index: '3'")]
        public void ConditionPatternParser_AssertBadInputThrowsFormatException(string input, string expected)
        {
            var ex = Assert.Throws<FormatException>(() => new ConditionPatternParser().ParseActionCondition(input));
            Assert.Equal(expected, ex.Message);
        }

        private bool CompareConditions(ParsedModRewriteInput i1, ParsedModRewriteInput i2)
        {
            if (i1.OperationType != i2.OperationType ||
                i1.ConditionType != i2.ConditionType ||
                i1.Operand != i2.Operand ||
                i1.Invert != i2.Invert)
            {
                return false;
            }
            return true;
        }
    }
}
