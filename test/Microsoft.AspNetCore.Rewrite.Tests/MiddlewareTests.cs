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
            var options = new RewriteOptions().AddRewrite("(.*)", "http://example.com/$1", skipRemainingRules: false);
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

            var response = await server.CreateClient().GetStringAsync("foo");

            Assert.Equal("http://example.com/foo", response);
        }

        [Fact]
        public async Task CheckRedirectPath()
        {
            var options = new RewriteOptions().AddRedirect("(.*)","http://example.com/$1", statusCode: 301);
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("foo");

            Assert.Equal("http://example.com/foo", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task CheckRedirectToHttps()
        {
            var options = new RewriteOptions().AddRedirectToHttps(statusCode: 301);
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
        public async Task CheckIfEmptyStringRedirectCorrectly()
        {
            var options = new RewriteOptions().AddRedirect("(.*)", "$1", statusCode: 301);
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");
            Assert.Equal(response.Headers.Location.OriginalString, "/");
        }

        [Fact]
        public async Task CheckIfEmptyStringRewriteCorrectly()
        {
            var options = new RewriteOptions().AddRewrite("(.*)", "$1", skipRemainingRules: false);
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
                app.Run(context => context.Response.WriteAsync(
                        context.Request.Path +
                        context.Request.QueryString));
            });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetStringAsync("");

            Assert.Equal(response, "/");
        }

        [Fact]
        public async Task SettingPathBase()
        {
            var options = new RewriteOptions().AddRedirect("(.*)", "$1");
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.UseRewriter(options);
                app.Run(context => context.Response.WriteAsync(
                        context.Request.Path +
                        context.Request.QueryString));
            });
            var server = new TestServer(builder) {BaseAddress = new Uri("http://localhost:5000/foo")};

            var response = await server.CreateClient().GetAsync("");

            Assert.Equal(response.Headers.Location.OriginalString, "/foo");
        }
    }
}
