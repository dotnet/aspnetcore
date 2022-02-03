// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite;

public class ConditionPatternParserTest
{
    [Theory]
    [InlineData(">hey", (int)OperationType.Greater, "hey", (int)ConditionType.StringComp)]
    [InlineData("<hey", (int)OperationType.Less, "hey", (int)ConditionType.StringComp)]
    [InlineData(">=hey", (int)OperationType.GreaterEqual, "hey", (int)ConditionType.StringComp)]
    [InlineData("<=hey", (int)OperationType.LessEqual, "hey", (int)ConditionType.StringComp)]
    [InlineData("=hey", (int)OperationType.Equal, "hey", (int)ConditionType.StringComp)]
    public void ConditionPatternParser_CheckStringComp(string condition, int operation, string variable, int conditionType)
    {
        var results = ConditionPatternParser.ParseActionCondition(condition);

        var expected = new ParsedModRewriteInput { OperationType = (OperationType)operation, ConditionType = (ConditionType)conditionType, Operand = variable, Invert = false };
        Assert.True(CompareConditions(expected, results));
    }

    [Fact]
    public void ConditionPatternParser_CheckRegexEqual()
    {
        var condition = @"(.*)";
        var results = ConditionPatternParser.ParseActionCondition(condition);

        var expected = new ParsedModRewriteInput { ConditionType = ConditionType.Regex, Operand = "(.*)", Invert = false };
        Assert.True(CompareConditions(expected, results));
    }

    [Theory]
    [InlineData("-d", (int)OperationType.Directory, (int)ConditionType.PropertyTest)]
    [InlineData("-f", (int)OperationType.RegularFile, (int)ConditionType.PropertyTest)]
    [InlineData("-F", (int)OperationType.ExistingFile, (int)ConditionType.PropertyTest)]
    [InlineData("-h", (int)OperationType.SymbolicLink, (int)ConditionType.PropertyTest)]
    [InlineData("-L", (int)OperationType.SymbolicLink, (int)ConditionType.PropertyTest)]
    [InlineData("-l", (int)OperationType.SymbolicLink, (int)ConditionType.PropertyTest)]
    [InlineData("-s", (int)OperationType.Size, (int)ConditionType.PropertyTest)]
    [InlineData("-U", (int)OperationType.ExistingUrl, (int)ConditionType.PropertyTest)]
    [InlineData("-x", (int)OperationType.Executable, (int)ConditionType.PropertyTest)]
    public void ConditionPatternParser_CheckFileOperations(string condition, int operation, int cond)
    {
        var results = ConditionPatternParser.ParseActionCondition(condition);

        var expected = new ParsedModRewriteInput { ConditionType = (ConditionType)cond, OperationType = (OperationType)operation, Invert = false };
        Assert.True(CompareConditions(expected, results));
    }

    [Theory]
    [InlineData("!-d", (int)OperationType.Directory, (int)ConditionType.PropertyTest)]
    [InlineData("!-f", (int)OperationType.RegularFile, (int)ConditionType.PropertyTest)]
    [InlineData("!-F", (int)OperationType.ExistingFile, (int)ConditionType.PropertyTest)]
    [InlineData("!-h", (int)OperationType.SymbolicLink, (int)ConditionType.PropertyTest)]
    [InlineData("!-L", (int)OperationType.SymbolicLink, (int)ConditionType.PropertyTest)]
    [InlineData("!-l", (int)OperationType.SymbolicLink, (int)ConditionType.PropertyTest)]
    [InlineData("!-s", (int)OperationType.Size, (int)ConditionType.PropertyTest)]
    [InlineData("!-U", (int)OperationType.ExistingUrl, (int)ConditionType.PropertyTest)]
    [InlineData("!-x", (int)OperationType.Executable, (int)ConditionType.PropertyTest)]
    public void ConditionPatternParser_CheckFileOperationsInverted(string condition, int operation, int cond)
    {
        var results = ConditionPatternParser.ParseActionCondition(condition);

        var expected = new ParsedModRewriteInput { ConditionType = (ConditionType)cond, OperationType = (OperationType)operation, Invert = true };
        Assert.True(CompareConditions(expected, results));
    }

    [Theory]
    [InlineData("-gt1", (int)OperationType.Greater, "1", (int)ConditionType.IntComp)]
    [InlineData("-lt1", (int)OperationType.Less, "1", (int)ConditionType.IntComp)]
    [InlineData("-ge1", (int)OperationType.GreaterEqual, "1", (int)ConditionType.IntComp)]
    [InlineData("-le1", (int)OperationType.LessEqual, "1", (int)ConditionType.IntComp)]
    [InlineData("-eq1", (int)OperationType.Equal, "1", (int)ConditionType.IntComp)]
    [InlineData("-ne1", (int)OperationType.NotEqual, "1", (int)ConditionType.IntComp)]
    public void ConditionPatternParser_CheckIntComp(string condition, int operation, string variable, int cond)
    {
        var results = ConditionPatternParser.ParseActionCondition(condition);

        var expected = new ParsedModRewriteInput { ConditionType = (ConditionType)cond, OperationType = (OperationType)operation, Invert = false, Operand = variable };
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
        var ex = Assert.Throws<FormatException>(() => ConditionPatternParser.ParseActionCondition(input));
        Assert.Equal(expected, ex.Message);
    }

    private static bool CompareConditions(ParsedModRewriteInput i1, ParsedModRewriteInput i2)
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
