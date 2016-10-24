// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal;
using Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlRewrite
{
    public class ServerVariableTests
    {
        [Theory]
        [InlineData("CONTENT_LENGTH", "10")]
        [InlineData("CONTENT_TYPE", "json")]
        [InlineData("HTTP_ACCEPT", "accept")]
        [InlineData("HTTP_COOKIE", "cookie")]
        [InlineData("HTTP_HOST", "example.com")]
        [InlineData("HTTP_REFERER", "referer")]
        [InlineData("HTTP_USER_AGENT", "useragent")]
        [InlineData("HTTP_CONNECTION", "connection")]
        [InlineData("HTTP_URL", "/foo")]
        [InlineData("QUERY_STRING", "?bar=1")]
        [InlineData("REQUEST_FILENAME", "/foo")]
        public void CheckServerVariableParsingAndApplication(string variable, string expected)
        {
            // Arrange and Act
            var testParserContext = new ParserContext("test");
            var serverVar = ServerVariables.FindServerVariable(variable, testParserContext);
            var lookup = serverVar.Evaluate(CreateTestHttpContext(), CreateTestRuleMatch(), CreateTestCondMatch());
            // Assert
            Assert.Equal(expected, lookup);
        }

        private RewriteContext CreateTestHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("example.com");
            context.Request.Path = PathString.FromUriComponent("/foo");
            context.Request.QueryString = QueryString.FromUriComponent("?bar=1");
            context.Request.ContentLength = 10;
            context.Request.ContentType = "json";
            context.Request.Headers[HeaderNames.Accept] = "accept";
            context.Request.Headers[HeaderNames.Cookie] = "cookie";
            context.Request.Headers[HeaderNames.Referer] = "referer";
            context.Request.Headers[HeaderNames.UserAgent] = "useragent";
            context.Request.Headers[HeaderNames.Connection] = "connection";
            return new RewriteContext { HttpContext = context };
        }

        private MatchResults CreateTestRuleMatch()
        {
            var match = Regex.Match("foo/bar/baz", "(.*)/(.*)/(.*)");
            return new MatchResults { BackReference = match.Groups, Success = match.Success };
        }

        private MatchResults CreateTestCondMatch()
        {
            var match = Regex.Match("foo/bar/baz", "(.*)/(.*)/(.*)");
            return new MatchResults { BackReference = match.Groups, Success = match.Success };
        }
    }
}
