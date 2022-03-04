// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.IISUrlRewrite
{
    public class ServerVariableTests
    {
        [Theory]
        [InlineData("CONTENT_LENGTH", "10", (int)UriMatchPart.Path)]
        [InlineData("CONTENT_TYPE", "json", (int)UriMatchPart.Path)]
        [InlineData("HTTP_ACCEPT", "accept", (int)UriMatchPart.Path)]
        [InlineData("HTTP_COOKIE", "cookie", (int)UriMatchPart.Path)]
        [InlineData("HTTP_HOST", "example.com", (int)UriMatchPart.Path)]
        [InlineData("HTTP_REFERER", "referer", (int)UriMatchPart.Path)]
        [InlineData("HTTP_USER_AGENT", "useragent", (int)UriMatchPart.Path)]
        [InlineData("HTTP_CONNECTION", "connection", (int)UriMatchPart.Path)]
        [InlineData("HTTP_URL", "/foo", (int)UriMatchPart.Path)]
        [InlineData("HTTP_URL", "http://example.com/foo?bar=1", (int)UriMatchPart.Full)]
        [InlineData("QUERY_STRING", "bar=1", (int)UriMatchPart.Path)]
        [InlineData("REQUEST_FILENAME", "/foo", (int)UriMatchPart.Path)]
        [InlineData("REQUEST_URI", "/foo", (int)UriMatchPart.Path)]
        [InlineData("REQUEST_URI", "http://example.com/foo?bar=1", (int)UriMatchPart.Full)]
        [InlineData("REQUEST_METHOD", "GET", (int)UriMatchPart.Full)]
        public void CheckServerVariableParsingAndApplication(string variable, string expected, int uriMatchPart)
        {
            // Arrange and Act
            var testParserContext = new ParserContext("test");
            var serverVar = ServerVariables.FindServerVariable(variable, testParserContext, (UriMatchPart)uriMatchPart, true);
            var lookup = serverVar.Evaluate(CreateTestRewriteContext(), CreateTestRuleMatch().BackReferences, CreateTestCondMatch().BackReferences);
            // Assert
            Assert.Equal(expected, lookup);
        }

        [Theory]
        [InlineData("CONTENT_LENGTH", "20", (int)UriMatchPart.Path)]
        [InlineData("CONTENT_TYPE", "text/xml", (int)UriMatchPart.Path)]
        [InlineData("HTTP_ACCEPT", "other-accept", (int)UriMatchPart.Path)]
        [InlineData("HTTP_COOKIE", "other-cookie", (int)UriMatchPart.Path)]
        [InlineData("HTTP_HOST", "otherexample.com", (int)UriMatchPart.Path)]
        [InlineData("HTTP_REFERER", "other-referer", (int)UriMatchPart.Path)]
        [InlineData("HTTP_USER_AGENT", "other-useragent", (int)UriMatchPart.Path)]
        [InlineData("HTTP_CONNECTION", "other-connection", (int)UriMatchPart.Path)]
        [InlineData("HTTP_URL", "http://otherexample.com/other-foo?bar=2", (int)UriMatchPart.Full)]
        [InlineData("HTTP_URL", "http://otherexample.com/other-foo?bar=2", (int)UriMatchPart.Path)]
        [InlineData("QUERY_STRING", "bar=2", (int)UriMatchPart.Path)]
        [InlineData("REQUEST_FILENAME", "/other-foo", (int)UriMatchPart.Path)]
        [InlineData("REQUEST_URI", "/other-foo", (int)UriMatchPart.Path)]
        [InlineData("REQUEST_URI", "/other-foo", (int)UriMatchPart.Full)]
        [InlineData("REQUEST_METHOD", "POST", (int)UriMatchPart.Full)]
        public void CheckServerVariableFeatureHasPrecedenceWhenEnabled(string variable, string expected, int uriMatchPart)
        {
            // Arrange and Act
            var testParserContext = new ParserContext("test");
            var serverVar = ServerVariables.FindServerVariable(variable, testParserContext, (UriMatchPart)uriMatchPart, false);
            var httpContext = CreateTestHttpContext();
            httpContext.Features.Set<IServerVariablesFeature>(new TestServerVariablesFeature(new Dictionary<string, string>
            {
                ["CONTENT_LENGTH"] = "20",
                ["CONTENT_TYPE"] = "text/xml",
                ["HTTP_ACCEPT"] = "other-accept",
                ["HTTP_COOKIE"] = "other-cookie",
                ["HTTP_HOST"] = "otherexample.com",
                ["HTTP_REFERER"] = "other-referer",
                ["HTTP_USER_AGENT"] = "other-useragent",
                ["HTTP_CONNECTION"] = "other-connection",
                ["HTTP_URL"] = "http://otherexample.com/other-foo?bar=2",
                ["QUERY_STRING"] = "bar=2",
                ["REQUEST_FILENAME"] = "/other-foo",
                ["REQUEST_URI"] = "/other-foo",
                ["REQUEST_METHOD"] = "POST"
            }));

            var rewriteContext = CreateTestRewriteContext(httpContext);
            var lookup = serverVar.Evaluate(rewriteContext, CreateTestRuleMatch().BackReferences, CreateTestCondMatch().BackReferences);

            // Assert
            Assert.Equal(expected, lookup);
        }

        [Theory]
        [InlineData("CONTENT_LENGTH", "10", (int)UriMatchPart.Path)]
        [InlineData("CONTENT_TYPE", "json", (int)UriMatchPart.Path)]
        [InlineData("HTTP_ACCEPT", "accept", (int)UriMatchPart.Path)]
        [InlineData("HTTP_COOKIE", "cookie", (int)UriMatchPart.Path)]
        [InlineData("HTTP_HOST", "example.com", (int)UriMatchPart.Path)]
        [InlineData("HTTP_REFERER", "referer", (int)UriMatchPart.Path)]
        [InlineData("HTTP_USER_AGENT", "useragent", (int)UriMatchPart.Path)]
        [InlineData("HTTP_CONNECTION", "connection", (int)UriMatchPart.Path)]
        [InlineData("HTTP_URL", "/foo", (int)UriMatchPart.Path)]
        [InlineData("HTTP_URL", "http://example.com/foo?bar=1", (int)UriMatchPart.Full)]
        [InlineData("QUERY_STRING", "bar=1", (int)UriMatchPart.Path)]
        [InlineData("REQUEST_FILENAME", "/foo", (int)UriMatchPart.Path)]
        [InlineData("REQUEST_URI", "/foo", (int)UriMatchPart.Path)]
        [InlineData("REQUEST_URI", "http://example.com/foo?bar=1", (int)UriMatchPart.Full)]
        [InlineData("REQUEST_METHOD", "GET", (int)UriMatchPart.Full)]
        public void CheckServerVariableFeatureIsntUsedWhenDisabled(string variable, string expected, int uriMatchPart)
        {
            // Arrange and Act
            var testParserContext = new ParserContext("test");
            var serverVar = ServerVariables.FindServerVariable(variable, testParserContext, (UriMatchPart)uriMatchPart, true);
            var httpContext = CreateTestHttpContext();
            httpContext.Features.Set<IServerVariablesFeature>(new TestServerVariablesFeature(new Dictionary<string, string>
            {
                ["CONTENT_LENGTH"] = "20",
                ["CONTENT_TYPE"] = "text/xml",
                ["HTTP_ACCEPT"] = "other-accept",
                ["HTTP_COOKIE"] = "other-cookie",
                ["HTTP_HOST"] = "otherexample.com",
                ["HTTP_REFERER"] = "other-referer",
                ["HTTP_USER_AGENT"] = "other-useragent",
                ["HTTP_CONNECTION"] = "other-connection",
                ["HTTP_URL"] = "http://otherexample.com/other-foo?bar=2",
                ["QUERY_STRING"] = "bar=2",
                ["REQUEST_FILENAME"] = "/other-foo",
                ["REQUEST_URI"] = "/other-foo",
                ["REQUEST_METHOD"] = "POST"
            }));

            var rewriteContext = CreateTestRewriteContext(httpContext);
            var lookup = serverVar.Evaluate(rewriteContext, CreateTestRuleMatch().BackReferences, CreateTestCondMatch().BackReferences);

            // Assert
            Assert.Equal(expected, lookup);
        }

        private HttpContext CreateTestHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Scheme = "http";
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

            return context;
        }

        private RewriteContext CreateTestRewriteContext(HttpContext context = null)
        {
            return new RewriteContext { HttpContext = context ?? CreateTestHttpContext() };
        }

        private MatchResults CreateTestRuleMatch()
        {
            var match = Regex.Match("foo/bar/baz", "(.*)/(.*)/(.*)");
            return new MatchResults { BackReferences = new BackReferenceCollection(match.Groups), Success = match.Success };
        }

        private MatchResults CreateTestCondMatch()
        {
            var match = Regex.Match("foo/bar/baz", "(.*)/(.*)/(.*)");
            return new MatchResults { BackReferences = new BackReferenceCollection(match.Groups), Success = match.Success };
        }

        [Fact]
        private void EmptyQueryStringCheck()
        {
            var context = new DefaultHttpContext();
            var rewriteContext = new RewriteContext { HttpContext = context };
            var testParserContext = new ParserContext("test");
            var serverVar = ServerVariables.FindServerVariable("QUERY_STRING", testParserContext, UriMatchPart.Path, true);
            var lookup = serverVar.Evaluate(rewriteContext, CreateTestRuleMatch().BackReferences, CreateTestCondMatch().BackReferences);

            Assert.Equal(string.Empty, lookup);
        }
    }
}
