// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;
using Microsoft.AspNetCore.Rewrite.UrlActions;
using Microsoft.AspNetCore.Rewrite.UrlMatches;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlRewrite
{
    public class MiddlewareTests
    {
        [Fact]
        public async Task Invoke_RedirectPathToPathAndQuery()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Rewrite to article.aspx"">
                <match url = ""^article/([0-9]+)/([_0-9a-z-]+)"" />
                <action type=""Redirect"" url =""article.aspx?id={R:1}&amp;title={R:2}"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(context.Response.Headers[HeaderNames.Location]));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("article/10/hey");

            Assert.Equal("/article.aspx?id=10&title=hey", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task Invoke_RewritePathToPathAndQuery()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Rewrite to article.aspx"">
                <match url = ""^article/([0-9]+)/([_0-9a-z-]+)"" />
                <action type=""Rewrite"" url =""article.aspx?id={R:1}&amp;title={R:2}"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(context.Request.Path + context.Request.QueryString));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("/article/10/hey");

            Assert.Equal("/article.aspx?id=10&title=hey", response);
        }

        [Fact]
        public async Task Invoke_RewriteBasedOnQueryStringParameters()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Query String Rewrite"">
                <match url=""page\.asp$"" />
                <conditions>
                <add input=""{QUERY_STRING}"" pattern=""p1=(\d+)"" />
                <add input=""##{C:1}##_{QUERY_STRING}"" pattern=""##([^#]+)##_.*p2=(\d+)"" />
                </conditions>
                <action type=""Rewrite"" url=""newpage.aspx?param1={C:1}&amp;param2={C:2}"" appendQueryString=""false""/>
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(context.Request.Path + context.Request.QueryString));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("page.asp?p2=321&p1=123");

            Assert.Equal("/newpage.aspx?param1=123&param2=321", response);
        }

        [Fact]
        public async Task Invoke_RedirectToLowerCase()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Convert to lower case"" stopProcessing=""true"">
                <match url="".*[A-Z].*"" ignoreCase=""false"" />
                <action type=""Redirect"" url=""{ToLower:{R:0}}"" redirectType=""Permanent"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(context.Response.Headers[HeaderNames.Location]));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("HElLo");

            Assert.Equal("/hello", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task Invoke_RedirectRemoveTrailingSlash()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Remove trailing slash"" stopProcessing=""true"">
                <match url=""(.*)/$"" />
                <conditions>
                <add input=""{REQUEST_FILENAME}"" matchType=""IsFile"" negate=""true"" />
                <add input=""{REQUEST_FILENAME}"" matchType=""IsDirectory"" negate=""true"" />
                </conditions>
                <action type=""Redirect"" redirectType=""Permanent"" url=""{R:1}"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("hey/hello/");

            Assert.Equal("/hey/hello", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task Invoke_RedirectAddTrailingSlash()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Add trailing slash"" stopProcessing=""true"">
                <match url=""(.*[^/])$"" />
                <conditions>
                <add input=""{REQUEST_FILENAME}"" matchType=""IsFile"" negate=""true"" />
                <add input=""{REQUEST_FILENAME}"" matchType=""IsDirectory"" negate=""true"" />
                </conditions>
                <action type=""Redirect"" redirectType=""Permanent"" url=""{R:1}/"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("hey/hello");

            Assert.Equal("/hey/hello/", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task Invoke_RedirectToHttps()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Redirect to HTTPS"" stopProcessing=""true"">
                <match url=""(.*)"" />
                <conditions>
                <add input=""{HTTPS}"" pattern=""^OFF$"" />
                </conditions>
                <action type=""Redirect"" url=""https://{HTTP_HOST}/{R:1}"" redirectType=""Permanent"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(new Uri("http://example.com"));

            Assert.Equal("https://example.com/", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task Invoke_RewriteToHttps()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Rewrite to HTTPS"" stopProcessing=""true"">
                <match url=""(.*)"" />
                <conditions>
                <add input=""{HTTPS}"" pattern=""^OFF$"" />
                </conditions>
                <action type=""Rewrite"" url=""https://{HTTP_HOST}/{R:1}"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                        context.Request.Scheme +
                        "://" +
                        context.Request.Host +
                        context.Request.Path +
                        context.Request.QueryString));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync(new Uri("http://example.com"));

            Assert.Equal("https://example.com/", response);
        }

        [Fact]
        public async Task Invoke_ReverseProxyToAnotherSite()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Proxy"">
                <match url=""(.*)"" />
                <action type=""Rewrite"" url=""http://internalserver/{R:1}"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                        context.Request.Scheme +
                        "://" +
                        context.Request.Host +
                        context.Request.Path +
                        context.Request.QueryString));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync(new Uri("http://example.com/"));

            Assert.Equal("http://internalserver/", response);
        }

        [Fact]
        public async Task Invoke_CaptureEmptyStringInRegexAssertRedirectLocationHasForwardSlash()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"">
                <match url=""(.*)"" />
                <action type=""Redirect"" url=""{R:1}"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                        context.Request.Scheme +
                        "://" +
                        context.Request.Host +
                        context.Request.Path +
                        context.Request.QueryString));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(new Uri("http://example.com/"));

            Assert.Equal("/", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task Invoke_CaptureEmptyStringInRegexAssertRewriteLocationHasForwardSlash()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"">
                <match url=""(.*)"" />
                <action type=""Rewrite"" url=""{R:1}"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                        context.Request.Path +
                        context.Request.QueryString));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync(new Uri("http://example.com/"));

            Assert.Equal("/", response);
        }

        [Fact]
        public async Task Invoke_CaptureEmptyStringInRegexAssertLocationHeaderContainsPathBase()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"">
                <match url=""(.*)"" />
                <action type=""Redirect"" url=""{R:1}"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
                app.Run(context => context.Response.WriteAsync(
                        context.Request.Path +
                        context.Request.QueryString));
            });
            var server = new TestServer(builder) { BaseAddress = new Uri("http://localhost:5000/foo") };

            var response = await server.CreateClient().GetAsync("");

            Assert.Equal("/foo", response.Headers.Location.OriginalString);
        }

        [Theory]
        [InlineData("IsFile")]
        [InlineData("isfile")]
        [InlineData("IsDirectory")]
        [InlineData("isdirectory")]
        public async Task VerifyIsFileAndIsDirectoryParsing(string matchType)
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader($@"<rewrite>
                <rules>
                <rule name=""Test"">
                <match url=""(.*[^/])$"" />
                <conditions>
                <add input=""{{REQUEST_FILENAME}}"" matchType=""{matchType}"" negate=""true""/>
                </conditions>
                <action type=""Redirect"" url=""{{R:1}}/"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("hey/hello");

            Assert.Equal("/hey/hello/", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task VerifyTrackAllCaptures()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"">
                <match url=""(.*)"" ignoreCase=""false"" />
                <conditions trackAllCaptures = ""true"" >
                <add input=""{REQUEST_URI}"" pattern=""^/([a-zA-Z]+)/([0-9]+)$"" />
                <add input=""{QUERY_STRING}"" pattern=""p2=([a-z]+)"" />
                </conditions>
                <action type=""Redirect"" url =""blogposts/{C:1}/{C:4}"" />
                <!--rewrite action uses back - references to both conditions -->
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("article/23?p1=123&p2=abc");

            Assert.Equal("/blogposts/article/abc", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task VerifyTrackAllCapturesRuleAndConditionCapture()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"">
                <match url=""(.*)"" ignoreCase=""false"" />
                <conditions trackAllCaptures = ""true"" >
                <add input=""{REQUEST_URI}"" pattern=""^/([a-zA-Z]+)/([0-9]+)$"" />
                <add input=""{QUERY_STRING}"" pattern=""p2=([a-z]+)"" />
                </conditions>
                <action type=""Redirect"" url =""blog/{R:0}/{C:4}"" />
                <!--rewrite action uses back - references to both conditions -->
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("article/23?p1=123&p2=abc");

            Assert.Equal("/blog/article/23/abc", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ThrowIndexOutOfRangeExceptionWithCorrectMessage()
        {
            // Arrange, Act, Assert
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"">
                <match url=""(.*)"" ignoreCase=""false"" />
                <conditions trackAllCaptures = ""true"" >
                <add input=""{REQUEST_URI}"" pattern=""^/([a-zA-Z]+)/([0-9]+)$"" />
                <add input=""{QUERY_STRING}"" pattern=""p2=([a-z]+)"" />
                </conditions>
                <action type=""Redirect"" url =""blog/{R:0}/{C:9}"" />
                <!--rewrite action uses back - references to both conditions -->
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var ex = await Assert.ThrowsAsync<IndexOutOfRangeException>(() => server.CreateClient().GetAsync("article/23?p1=123&p2=abc"));

            Assert.Equal("Cannot access back reference at index 9. Only 5 back references were captured.", ex.Message);
        }

        [Fact]
        public async Task Invoke_GlobalRuleConditionMatchesAgainstFullUri_ParsedRule()
        {
            // arrange
            var xml = @"<rewrite>
                            <globalRules>
                                <rule name=""Test"" patternSyntax=""ECMAScript"" stopProcessing=""true"">
                                    <match url="".*"" />
                                    <conditions logicalGrouping=""MatchAll"" trackAllCaptures=""false"">
                                        <add input=""{REQUEST_URI}"" pattern=""^http://localhost/([0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12})(/.*)"" />
                                    </conditions>
                                    <action type=""Rewrite"" url=""http://www.test.com{C:2}"" />
                                </rule>
                            </globalRules>
                        </rewrite>";
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(xml));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(context.Request.GetEncodedUrl()));
                });
            var server = new TestServer(builder);

            // act
            var response = await server.CreateClient().GetStringAsync($"http://localhost/{Guid.NewGuid()}/foo/bar");

            // assert
            Assert.Equal("http://www.test.com/foo/bar", response);
        }

        [Theory]
        [InlineData("http://fetch.environment.local/dev/path", "http://1.1.1.1/path")]
        [InlineData("http://fetch.environment.local/qa/path", "http://fetch.environment.local/qa/path")]
        public async Task Invoke_ReverseProxyToAnotherSiteUsingXmlConfiguredRewriteMap(string requestUri, string expectedRewrittenUri)
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"
                <rewrite>
                    <rules>
                        <rule name=""Proxy"">
                            <match url=""([^/]*)(/?.*)"" />
                            <conditions>
                                <add input=""{environmentMap:{R:1}}"" pattern=""(.+)"" />
                            </conditions>
                            <action type=""Rewrite"" url=""http://{C:1}{R:2}"" appendQueryString=""true"" />
                        </rule>
                    </rules>
                    <rewriteMaps>
                        <rewriteMap name=""environmentMap"">
                            <add key=""dev"" value=""1.1.1.1"" />
                        </rewriteMap>
                    </rewriteMaps>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(context.Request.GetEncodedUrl()));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync(new Uri(requestUri));

            Assert.Equal(expectedRewrittenUri, response);
        }

        [Fact]
        public async Task Invoke_CustomResponse()
        {
            var options = new RewriteOptions().AddIISUrlRewrite(new StringReader(@"<rewrite>
                <rules>
                <rule name=""Forbidden"">
                <match url = "".*"" />
                <action type=""CustomResponse"" statusCode=""403"" statusReason=""reason"" statusDescription=""description"" />
                </rule>
                </rules>
                </rewrite>"));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("article/10/hey");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("reason", response.ReasonPhrase);
            Assert.Equal("description", content);
        }

        [Theory]
        [InlineData(@"^http://localhost(/.*)", "http://localhost/foo/bar", (int)UriMatchPart.Path)]
        [InlineData(@"^http://localhost(/.*)", "http://www.test.com/foo/bar", (int)UriMatchPart.Full)]
        public async Task Invoke_GlobalRuleConditionMatchesAgainstFullUri_CodedRule(string conditionInputPattern, string expectedResult, int uriMatchPart)
        {
            // arrange
            var inputParser = new InputParser();

            var ruleBuilder = new UrlRewriteRuleBuilder
            {
                Name = "test",
                Global = false
            };
            ruleBuilder.AddUrlMatch(".*");

            var condition = new UriMatchCondition(
                inputParser,
                "{REQUEST_URI}",
                conditionInputPattern,
                (UriMatchPart)uriMatchPart,
                ignoreCase: true,
                negate: false);
            ruleBuilder.ConfigureConditionBehavior(LogicalGrouping.MatchAll, trackAllCaptures: true);
            ruleBuilder.AddUrlCondition(condition);

            var action = new RewriteAction(
                RuleResult.SkipRemainingRules,
                inputParser.ParseInputString(@"http://www.test.com{C:1}", (UriMatchPart)uriMatchPart),
                queryStringAppend: false);
            ruleBuilder.AddUrlAction(action);

            var options = new RewriteOptions().Add(ruleBuilder.Build());
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(context.Request.GetEncodedUrl()));
                });
            var server = new TestServer(builder);

            // act
            var response = await server.CreateClient().GetStringAsync("http://localhost/foo/bar");

            // assert
            Assert.Equal(expectedResult, response);
        }
    }
}
