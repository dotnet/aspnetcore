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

    [Fact]
    public void Tokenize_RegexDigitShorthand_DoesNotThrow()
    {
        var testString = @"RewriteRule ^/(\d)$ /?num=\$1";

        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add("RewriteRule");
        expected.Add(@"^/(\d)$");
        expected.Add(@"/?num=\$1");
        Assert.Equal(expected, tokens);
    }

    [Fact]
    public void Tokenize_RegexWordShorthand_DoesNotThrow()
    {
        var testString = @"RewriteRule ^/(\w+)$ /?word=\$1";

        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add("RewriteRule");
        expected.Add(@"^/(\w+)$");
        expected.Add(@"/?word=\$1");
        Assert.Equal(expected, tokens);
    }

    [Fact]
    public void Tokenize_RegexWhitespaceShorthand_DoesNotThrow()
    {
        var testString = @"RewriteRule ^/(\s+)$ /?s=\$1";

        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add("RewriteRule");
        expected.Add(@"^/(\s+)$");
        expected.Add(@"/?s=\$1");
        Assert.Equal(expected, tokens);
    }

    [Fact]
    public void Tokenize_EscapedSpaceStillWorks_AfterFix()
    {
        var testString = @"RewriteRule ^/foo\ bar$ /result";

        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add("RewriteRule");
        expected.Add("^/foo bar$");
        expected.Add("/result");
        Assert.Equal(expected, tokens);
    }

    [Fact]
    public void Tokenize_RegexShorthandAndEscapedSpace_BothWork()
    {
        var testString = @"RewriteRule ^/(\d)\ test$ /result";

        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add("RewriteRule");
        expected.Add(@"^/(\d) test$");
        expected.Add("/result");
        Assert.Equal(expected, tokens);
    }
    [Fact]
    public void Tokenize_RegexShorthandInsideQuotes_DoesNotThrow()
    {
        var testString = "RewriteRule \"^/(\\d)$\" /result";

        var tokens = Tokenizer.Tokenize(testString);

        var expected = new List<string>();
        expected.Add("RewriteRule");
        expected.Add(@"^/(\d)$");
        expected.Add("/result");
        Assert.Equal(expected, tokens);
    }
}