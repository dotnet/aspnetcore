// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.CodeRules
{
    public class MiddlewareTests
    {
        [Fact]
        public async Task CheckRewritePath()
        {
            var options = new RewriteOptions().RewriteRule("(.*)", "http://example.com/{R:1}");
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                        context.Request.Scheme +
                        "://" +
                        context.Request.Host +
                        context.Request.Path +
                        context.Request.QueryString));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("foo");

            Assert.Equal(response, "http://example.com/foo");
        }

        [Fact]
        public async Task CheckRedirectPath()
        {
            var options = new RewriteOptions().RedirectRule("(.*)","http://example.com/{R:1}", statusCode: 301);
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("foo");

            Assert.Equal(response.Headers.Location.OriginalString, "http://example.com/foo");
        }

        [Fact]
        public async Task CheckRewriteToHttps()
        {
            var options = new RewriteOptions().RewriteToHttps();
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);
                    app.UseRewriter(options);
                    app.Run(context => context.Response.WriteAsync(
                        context.Request.Scheme));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync(new Uri("http://example.com"));

            Assert.Equal(response, "https");
        }

        [Fact]
        public async Task CheckRedirectToHttps()
        {
            var options = new RewriteOptions().RedirectToHttps(statusCode: 301);
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync(new Uri("http://example.com"));

            Assert.Equal(response.Headers.Location.OriginalString, "https://example.com/");
        }
    }
}
