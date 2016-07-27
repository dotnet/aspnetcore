// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
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
            Assert.Equal(tokens, expected);
        }

        [Fact]
        public void Tokenize_CheckEscapedSpaceIgnored()
        {
            // TODO need consultation on escape characters.
            var testString = @"RewriteCond %{HTTPS}\ what !-f";
            var tokens = Tokenizer.Tokenize(testString);

            var expected = new List<string>();
            expected.Add("RewriteCond");
            expected.Add(@"%{HTTPS}\ what"); // TODO maybe just have the space here? talking point
            expected.Add("!-f");
            Assert.Equal(tokens,expected);
        }
    }
}
