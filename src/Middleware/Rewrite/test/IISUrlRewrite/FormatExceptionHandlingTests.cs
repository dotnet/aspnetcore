// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlRewrite;

public class FormatExceptionHandlingTests
{
    [Theory]
    [InlineData(
@"<rewrite>
    <rules>
        <rule name=""Rewrite to article.aspx"">
            <match url = ""(.*)"" />
            <conditions>
                <add input=""{HTTPS}"" />
            </conditions>
            <action type=""Rewrite"" url =""foo"" />
        </rule>
    </rules>
</rewrite>",
        "Match does not have an associated pattern attribute in condition")]
    [InlineData(
@"<rewrite>
    <rules>
        <rule name=""Rewrite to article.aspx"">
            <match url = ""(.*)"" />
            <conditions>
                <add input=""{HTTPS}"" patternSyntax=""ExactMatch""/>
            </conditions>
            <action type=""Rewrite"" url =""foo"" />
        </rule>
    </rules>
</rewrite>",
        "Match does not have an associated pattern attribute in condition")]
    [InlineData(
@"<rewrite>
    <rules>
        <rule name=""Rewrite to article.aspx"">
            <match url = ""(.*)"" />
            <conditions>
                <add input=""{HTTPS"" pattern=""^OFF$"" />
            </conditions>
            <action type=""Rewrite"" url =""foo"" />
        </rule>
    </rules>
</rewrite>",
        "Missing close brace for parameter at string index: '6'")]
    [InlineData(
@"<rewrite>
    <rules>
        <rule name=""Rewrite to article.aspx"">
            <match url = ""(.*)"" />
            <action type=""Rewrite"" url =""{"" />
        </rule>
    </rules>
</rewrite>",
        "Missing close brace for parameter at string index: '1'")]
    public void ThrowFormatExceptionWithCorrectMessage(string input, string expected)
    {
        // Arrange, Act, Assert
        var ex = Assert.Throws<FormatException>(() => new UrlRewriteFileParser().Parse(new StringReader(input), false));
        Assert.Equal(expected, ex.Message);
    }
}
