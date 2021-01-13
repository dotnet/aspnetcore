// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    public class ModRewriteMiddlewareTest
    {
        [Fact]
        public async Task Invoke_RewritePathWhenMatching()
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader("RewriteRule /hey/(.*) /$1 "));
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(context.Request.Path));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("/hey/hello");

            Assert.Equal("/hello", response);
        }

        [Fact]
        public async Task Invoke_RewritePathTerminatesOnFirstSuccessOfRule()
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader("RewriteRule /hey/(.*) /$1 [L]"))
                            .AddApacheModRewrite(new StringReader("RewriteRule /hello /what"));
            var builder = new WebHostBuilder()
                 .Configure(app =>
                 {
                     app.UseRewriter(options);
                     app.Run(context => context.Response.WriteAsync(context.Request.Path));
                 });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("/hey/hello");

            Assert.Equal("/hello", response);
        }

        [Fact]
        public async Task Invoke_RewritePathDoesNotTerminateOnFirstSuccessOfRule()
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader("RewriteRule /hey/(.*) /$1"))
                                       .AddApacheModRewrite(new StringReader("RewriteRule /hello /what"));
            var builder = new WebHostBuilder()
                 .Configure(app =>
                 {
                     app.UseRewriter(options);
                     app.Run(context => context.Response.WriteAsync(context.Request.Path));
                 });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("/hey/hello");

            Assert.Equal("/what", response);
        }

        [Fact]
        public async Task Invoke_ShouldIgnoreComments()
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader("#RewriteRule ^/hey/(.*) /$1 "));
            var builder = new WebHostBuilder()
                 .Configure(app =>
                 {
                     app.UseRewriter(options);
                     app.Run(context => context.Response.WriteAsync(context.Request.Path));
                 });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("/hey/hello");

            Assert.Equal("/hey/hello", response);
        }

        [Fact]
        public async Task Invoke_ShouldRewriteHomepage()
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader(@"RewriteRule ^/$ /homepage.html"));
            var builder = new WebHostBuilder()
                 .Configure(app =>
                 {
                     app.UseRewriter(options);
                     app.Run(context => context.Response.WriteAsync(context.Request.Path));
                 });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("http://www.foo.org/");

            Assert.Equal("/homepage.html", response);
        }

        [Fact]
        public async Task Invoke_ShouldIgnorePorts()
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader(@"RewriteRule ^/$ /homepage.html"));
            var builder = new WebHostBuilder()
              .Configure(app =>
              {
                  app.UseRewriter(options);
                  app.Run(context => context.Response.WriteAsync(context.Request.Path));
              });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("http://www.foo.org:42/");

            Assert.Equal("/homepage.html", response);
        }

        [Fact]
        public async Task Invoke_HandleNegatedRewriteRules()
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader(@"RewriteRule !^/$ /homepage.html"));
            var builder = new WebHostBuilder()
              .Configure(app =>
              {
                  app.UseRewriter(options);
                  app.Run(context => context.Response.WriteAsync(context.Request.Path));
              });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("http://www.foo.org/");

            Assert.Equal("/", response);
        }

        [Theory]
        [InlineData("http://www.foo.org/homepage.aspx", @"RewriteRule (.*)\.aspx $1.php", "/homepage.php")]
        [InlineData("http://www.foo.org/pages/homepage.aspx", @"RewriteRule (.*)/(.*)\.aspx $2.php", "/homepage.php")]
        public async Task Invoke_BackReferencesShouldBeApplied(string url, string rule, string expected)
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader(rule));
            var builder = new WebHostBuilder()
             .Configure(app =>
                 {
                     app.UseRewriter(options);
                     app.Run(context => context.Response.WriteAsync(context.Request.Path));
                 });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync(url);

            Assert.Equal(expected, response);
        }

        [Theory]
        [InlineData("http://www.foo.org/homepage.aspx", @"RewriteRule (.*)\.aspx $1.php", "/homepage.php")]
        [InlineData("http://www.foo.org/homepage.ASPX", @"RewriteRule (.*)\.aspx $1.php", "/homepage.ASPX")]
        [InlineData("http://www.foo.org/homepage.aspx", @"RewriteRule (.*)\.aspx $1.php [NC]", "/homepage.php")]
        [InlineData("http://www.foo.org/homepage.ASPX", @"RewriteRule (.*)\.aspx $1.php [NC]", "/homepage.php")]
        [InlineData("http://www.foo.org/homepage.aspx", @"RewriteRule (.*)\.aspx $1.php [nocase]", "/homepage.php")]
        [InlineData("http://www.foo.org/homepage.ASPX", @"RewriteRule (.*)\.aspx $1.php [nocase]", "/homepage.php")]
        public async Task Invoke_ShouldHandleFlagNoCase(string url, string rule, string expected)
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader(rule));
            var builder = new WebHostBuilder()
             .Configure(app =>
             {
                 app.UseRewriter(options);
                 app.Run(context => context.Response.WriteAsync(context.Request.Path));
             });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync(url);

            Assert.Equal(expected, response);
        }

        [Fact]
        public async Task Invoke_CheckFullUrlWithOnlyPath()
        {
            var options = new RewriteOptions()
                .AddApacheModRewrite(new StringReader(@"RewriteRule (.+) http://www.example.com$1/"));
            var builder = new WebHostBuilder()
              .Configure(app =>
              {
                  app.UseRewriter(options);
                  app.Run(context => context.Response.WriteAsync(context.Request.Path));
              });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("http://www.foo.org/blog/2016-jun");

            Assert.Equal(@"/blog/2016-jun/", response);
        }

        [Fact]
        public async Task Invoke_CheckFullUrlWithUFlag()
        {
            var options = new RewriteOptions()
                .AddApacheModRewrite(new StringReader(@"RewriteRule (.+) http://www.example.com$1/"));
            var builder = new WebHostBuilder()
              .Configure(app =>
              {
                  app.UseRewriter(options);
                  app.Run(context => context.Response.WriteAsync(context.Request.Scheme + "://" + context.Request.Host.Host + context.Request.Path + context.Request.QueryString));
              });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("http://www.foo.org/blog/2016-jun");

            Assert.Equal(@"http://www.example.com/blog/2016-jun/", response);
        }

        [Fact]
        public async Task Invoke_CheckModFileConditions()
        {
            var options = new RewriteOptions()
                .AddApacheModRewrite(new StringReader(@"RewriteRule (.+) http://www.example.com$1/"));
            var builder = new WebHostBuilder()
              .Configure(app =>
              {
                  app.UseRewriter(options);
                  app.Run(context => context.Response.WriteAsync(context.Request.Scheme + "://" + context.Request.Host.Host + context.Request.Path + context.Request.QueryString));
              });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("http://www.foo.org/blog/2016-jun");

            Assert.Equal(@"http://www.example.com/blog/2016-jun/", response);
        }

        [Theory]
        [InlineData("http://www.example.com/foo/")]
        public async Task Invoke_EnsureHttps(string input)
        {
            var options = new RewriteOptions()
                .AddApacheModRewrite(new StringReader("RewriteCond %{REQUEST_URI} /foo/  \nRewriteCond %{HTTPS} !on   \nRewriteRule ^(.*)$ https://www.example.com$1 [R=301,L]"));
            var builder = new WebHostBuilder()
              .Configure(app =>
              {
                  app.UseRewriter(options);
                  app.Run(context => context.Response.WriteAsync(context.Request.Scheme + "://" + context.Request.Host.Host + context.Request.Path + context.Request.QueryString));
              });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(input);

            Assert.Equal(response.StatusCode, (HttpStatusCode)301);
            Assert.Equal(@"https://www.example.com/foo/", response.Headers.Location.AbsoluteUri);
        }

        [Theory]
        [InlineData("http://www.example.com/")]
        public async Task Invoke_CaptureEmptyStringInRegexAssertRedirectLocationHasForwardSlash(string input)
        {
            var options = new RewriteOptions()
                .AddApacheModRewrite(new StringReader("RewriteRule ^(.*)$ $1 [R=301,L]"));
            var builder = new WebHostBuilder()
              .Configure(app =>
              {
                  app.UseRewriter(options);
                  app.Run(context => context.Response.WriteAsync(context.Request.Scheme + "://" + context.Request.Host.Host + context.Request.Path + context.Request.QueryString));
              });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(input);

            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.Equal("/", response.Headers.Location.OriginalString);
        }

        [Theory]
        [InlineData("http://www.example.com/")]
        public async Task Invoke_CaptureEmptyStringInRegexAssertRewriteHasForwardSlash(string input)
        {
            var options = new RewriteOptions()
                .AddApacheModRewrite(new StringReader("RewriteRule ^(.*)$ $1 [L]"));
            var builder = new WebHostBuilder()
              .Configure(app =>
              {
                  app.UseRewriter(options);
                  app.Run(context => context.Response.WriteAsync(context.Request.Path + context.Request.QueryString));
              });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync(input);
            Assert.Equal("/", response);
        }

        [Fact]
        public async Task Invoke_CaptureEmptyStringInRegexAssertLocationHeaderContainsPathBase()
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader(@"RewriteRule ^(.*)$ $1 [R=301,L]"));
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

        [Fact]
        public async Task CapturedVariablesInConditionsArePreservedToRewriteRule()
        {
            var options = new RewriteOptions().AddApacheModRewrite(new StringReader(@"RewriteCond %{REQUEST_URI} /home
RewriteCond %{QUERY_STRING} report_id=(.+)
RewriteRule (.*) http://localhost:80/home/report/%1 [R=301,L,QSD]"));
            var builder = new WebHostBuilder().Configure(app =>
               {
                   app.UseRewriter(options);
                   app.Run(context => context.Response.WriteAsync(
                           context.Request.Path +
                           context.Request.QueryString));
               });

            var server = new TestServer(builder) { BaseAddress = new Uri("http://localhost:5000/foo") };
            var response = await server.CreateClient().GetAsync("/home?report_id=123");

            Assert.Equal("http://localhost:80/home/report/123", response.Headers.Location.OriginalString);
        }
    }
}
