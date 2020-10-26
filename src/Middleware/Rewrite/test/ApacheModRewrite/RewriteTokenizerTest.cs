// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    public class RewriteTokenizerTest
    {
        [Fact]
        public void Tokenize_RewriteCondtion()
        {
            var testString = "RewriteCond %{HTTPS} !-f";
            var tokens = new Tokenizer().Tokenize(testString);

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
            var tokens = new Tokenizer().Tokenize(testString);

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
            var tokens = new Tokenizer().Tokenize(testString);

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
            var tokens = new Tokenizer().Tokenize(testString);

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
            var tokens = new Tokenizer().Tokenize(testString);

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
            var ex = Assert.Throws<FormatException>(() => new Tokenizer().Tokenize("\\"));
            Assert.Equal(@"Invalid escaper character in string: \", ex.Message);
        }

        [Fact]
        public void Tokenize_AssertFormatExceptionWhenUnevenNumberOfQuotes()
        {
            var ex = Assert.Throws<FormatException>(() => new Tokenizer().Tokenize("\""));
            Assert.Equal("Mismatched number of quotes: \"", ex.Message);
        }
    }
}
