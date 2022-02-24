// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite;

public class RewriteTokenizerTest
{
    [Fact]
    public void Tokenize_RewriteCondtion()
    {
        var testString = "RewriteCond %{HTTPS} !-f";
        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add("RewriteCond");
        expected.Add("%{HTTPS}");
        expected.Add("!-f");
        Assert.Equal(expected, tokens);
    }

    [Fact]
    public void Tokenize_CheckEscapedSpaceIgnored()
    {
        var testString = @"RewriteCond %{HTTPS}\ what !-f";
        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add("RewriteCond");
        expected.Add(@"%{HTTPS} what");
        expected.Add("!-f");
        Assert.Equal(expected, tokens);
    }

    [Fact]
    public void Tokenize_CheckWhiteSpaceDirectlyFollowedByEscapeCharacter_CorrectSplit()
    {
        var testString = @"RewriteCond %{HTTPS} \ what !-f";
        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add(@"RewriteCond");
        expected.Add(@"%{HTTPS}");
        expected.Add(@" what");
        expected.Add(@"!-f");
        Assert.Equal(expected, tokens);
    }

    [Fact]
    public void Tokenize_CheckWhiteSpaceAtEndOfString_CorrectSplit()
    {
        var testString = @"RewriteCond %{HTTPS} \ what !-f    ";
        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add(@"RewriteCond");
        expected.Add(@"%{HTTPS}");
        expected.Add(@" what");
        expected.Add(@"!-f");
        Assert.Equal(expected, tokens);
    }

    [Fact]
    public void Tokenize_CheckQuotesAreProperlyRemovedFromString()
    {
        var testString = "RewriteCond \"%{HTTPS}\" \"\\ what\" \"!-f\"    ";
        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add(@"RewriteCond");
        expected.Add(@"%{HTTPS}");
        expected.Add(@" what");
        expected.Add(@"!-f");
        Assert.Equal(expected, tokens);
    }

    [Fact]
    public void Tokenize_AssertFormatExceptionWhenEscapeCharacterIsAtEndOfString()
    {
        var ex = Assert.Throws<FormatException>(() => Tokenizer.Tokenize("\\"));
        Assert.Equal(@"Invalid escaper character in string: \", ex.Message);
    }

    [Fact]
    public void Tokenize_AssertFormatExceptionWhenUnevenNumberOfQuotes()
    {
        var ex = Assert.Throws<FormatException>(() => Tokenizer.Tokenize("\""));
        Assert.Equal("Mismatched number of quotes: \"", ex.Message);
    }
}
