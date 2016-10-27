// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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

            Assert.Equal(response.Headers.Location.OriginalString, "/foo");
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

            Assert.Equal("/hey/hello/", response.Headers.Location.OriginalString); ;
        }
    }
}
