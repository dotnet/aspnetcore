// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite;

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
        var ex = Assert.Throws<FormatException>(() => FileParser.Parse(new StringReader(input)));
        Assert.Equal(expected, ex.Message);
    }
}
