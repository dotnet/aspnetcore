// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlRewrite
{
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
}
