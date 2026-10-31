// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlRewrite;

public class InvalidUrlRewriteFormatExceptionHandlingTests
{
    [Theory]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Rewrite to article.aspx"">
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'Condition must have an associated match'. Line number '3': '10'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Rewrite to article.aspx"">
            <match />
            <action type=""Rewrite"" url=""foo"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'Match must have Url Attribute'. Line number '4': '14'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Rewrite to article.aspx"">
            <match url = ""(.*)"" />
            <conditions>
                <add pattern=""^OFF$"" />
            </conditions>
            <action type=""Rewrite"" url =""foo"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'Conditions must have an input attribute'. Line number '6': '18'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Rewrite to article.aspx"">
            <match url = ""(.*)"" />
            <action type=""Rewrite"" url ="""" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'Url attribute cannot contain an empty string'. Line number '5': '14'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"">
            <match url = ""(.*)/$"" />
            <action type=""Redirect"" redirectType=""foo"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The redirectType parameter 'foo' was not recognized'. Line number '5': '14'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"">
            <match url = ""(.*)/$"" />
            <action type=""foo"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The type parameter 'foo' was not recognized'. Line number '5': '14'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"">
            <match url = ""(.*)/$"" />
            <conditions logicalGrouping=""foo"">
                <add input=""{REQUEST_FILENAME}"" matchType=""isFile"" negate=""true""/>
            </conditions>
            <action type=""Redirect"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The logicalGrouping parameter 'foo' was not recognized'. Line number '5': '14'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"" patternSyntax=""foo"">
            <match url = ""(.*)/$"" />
            <action type=""Redirect"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The patternSyntax parameter 'foo' was not recognized'. Line number '3': '10'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"">
            <match url = ""(.*)/$"" />
            <conditions>
                <add input=""{REQUEST_FILENAME}"" matchType=""foo"" negate=""true""/>
            </conditions>
            <action type=""Redirect"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The matchType parameter 'foo' was not recognized'. Line number '6': '18'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"" enabled=""foo"">
            <match url = ""(.*)/$"" />
            <conditions>
                <add input=""{REQUEST_FILENAME}"" negate=""true""/>
            </conditions>
            <action type=""Redirect"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The enabled parameter 'foo' was not recognized'. Line number '3': '10'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"" stopProcessing=""foo"">
            <match url = ""(.*)/$"" />
            <conditions>
                <add input=""{REQUEST_FILENAME}"" negate=""true""/>
            </conditions>
            <action type=""Redirect"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The stopProcessing parameter 'foo' was not recognized'. Line number '3': '10'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"">
            <match url = ""(.*)/$"" ignoreCase=""foo""/>
            <conditions>
                <add input=""{REQUEST_FILENAME}"" negate=""true""/>
            </conditions>
            <action type=""Redirect"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The ignoreCase parameter 'foo' was not recognized'. Line number '4': '14'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"">
            <match url = ""(.*)/$""/>
            <conditions>
                <add input=""{REQUEST_FILENAME}"" ignoreCase=""foo""/>
            </conditions>
            <action type=""Redirect"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The ignoreCase parameter 'foo' was not recognized'. Line number '6': '18'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"">
            <match url = ""(.*)/$"" negate=""foo""/>
            <conditions>
                <add input=""{REQUEST_FILENAME}""/>
            </conditions>
            <action type=""Redirect"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The negate parameter 'foo' was not recognized'. Line number '4': '14'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"">
            <match url = ""(.*)/$""/>
            <conditions>
                <add input=""{REQUEST_FILENAME}"" negate=""foo""/>
            </conditions>
            <action type=""Redirect"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The negate parameter 'foo' was not recognized'. Line number '6': '18'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"">
            <match url = ""(.*)/$""/>
            <conditions trackAllCaptures=""foo"">
                <add input=""{REQUEST_FILENAME}""/>
            </conditions>
            <action type=""Redirect"" url =""{R:1}"" />
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The trackAllCaptures parameter 'foo' was not recognized'. Line number '5': '14'.")]
    [InlineData(
        @"<rewrite>
    <rules>
        <rule name=""Remove trailing slash"">
            <match url = ""(.*)/$""/>
            <action type=""Redirect"" url =""{R:1}"" appendQueryString=""foo""/>
        </rule>
    </rules>
</rewrite>",
        "Could not parse the UrlRewrite file. Message: 'The appendQueryString parameter 'foo' was not recognized'. Line number '5': '14'.")]
    [InlineData(
        "<rules><rule></rule></rules>",
        "Could not parse the UrlRewrite file. Message: 'The root element '<rewrite>' is missing'. Line number '0': '0'.")]
    public void ThrowInvalidUrlRewriteFormatExceptionWithCorrectMessage(string input, string expected)
    {
        // Arrange, Act, Assert
        var ex = Assert.Throws<InvalidUrlRewriteFormatException>(() => new UrlRewriteFileParser().Parse(new StringReader(input), false));
        Assert.Equal(expected, ex.Message);
    }
}
