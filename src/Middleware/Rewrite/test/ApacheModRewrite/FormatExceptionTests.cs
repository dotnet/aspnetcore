// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    public class FormatExceptionTests
    {
        [Theory]
        [InlineData(@"RewriteCond 1 2\", @"Invalid escaper character in string: RewriteCond 1 2\")]
        [InlineData("BadExpression 1 2 3 4", "Could not parse the mod_rewrite file. Message: 'Too many tokens on line'.  Line number '1'.")]
        [InlineData("RewriteCond % 2", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %{ 2", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %{asdf} 2", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %z 2", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond $ 2", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond $z 2", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %1 !", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %1 >", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %1 >=", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %1 <", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %1 <=", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %1 =", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %1 -", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %1 -a", "Could not parse the mod_rewrite file.  Line number '1'.")]
        [InlineData("RewriteCond %1 -getemp", "Could not parse the mod_rewrite file.  Line number '1'.")]
        public void ThrowFormatExceptionWithCorrectMessage(string input, string expected)
        {
            // Arrange, Act, Assert
            var ex = Assert.Throws<FormatException>(() => new FileParser().Parse(new StringReader(input)));
            Assert.Equal(expected, ex.Message);
        }
    }
}
