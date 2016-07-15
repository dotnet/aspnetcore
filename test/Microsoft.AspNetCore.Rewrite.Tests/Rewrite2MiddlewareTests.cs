// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite
{
    public class RewriteMiddlewareTests
    {

        [Theory(Skip = "Adapting to TestServer")]
        [InlineData("/foo", "", "/foo", "/yes")]
        [InlineData("/foo", "", "/foo/", "/yes")]
        [InlineData("/foo", "/Bar", "/foo", "/yes")]
        [InlineData("/foo", "/Bar", "/foo/cho", "/yes")]
        [InlineData("/foo", "/Bar", "/foo/cho/", "/yes")]
        [InlineData("/foo/cho", "/Bar", "/foo/cho", "/yes")]
        [InlineData("/foo/cho", "/Bar", "/foo/cho/do", "/yes")]
        public void PathMatchFunc_RewriteDone(string matchPath, string basePath, string requestPath, string rewrite)
        {
            var context = CreateRequest(basePath, requestPath);
            var options = new UrlRewriteOptions().RewritePath(matchPath, rewrite, false);
            var builder = new ApplicationBuilder(serviceProvider: null)
                .UseRewriter(options);
            var app = builder.Build();
            app.Invoke(context).Wait();
            Assert.Equal(rewrite, context.Request.Path);
        }
        [Theory(Skip = "Adapting to TestServer")]
        [InlineData(@"/(?<name>\w+)?/(?<id>\w+)?", @"", "/hey/hello", "/${id}/${name}", "/hello/hey")]
        [InlineData(@"/(?<name>\w+)?/(?<id>\w+)?/(?<temp>\w+)?", @"", "/hey/hello/what", "/${temp}/${id}/${name}", "/what/hello/hey")]
        public void PathMatchFunc_RegexRewriteDone(string matchPath, string basePath, string requestPath, string rewrite, string expected)
        {
            var context = CreateRequest(basePath, requestPath);
            var options = new UrlRewriteOptions().RewritePath(matchPath, rewrite, false);
            var builder = new ApplicationBuilder(serviceProvider: null)
                .UseRewriter(options);

            var app = builder.Build();
            app.Invoke(context).Wait();
            Assert.Equal(expected, context.Request.Path);
        }

        [Fact(Skip = "Adapting to TestServer")]
        public void PathMatchFunc_RedirectScheme()
        {
            HttpContext context = CreateRequest("/", "/");
            context.Request.Scheme = "http";
            var options = new UrlRewriteOptions().RedirectScheme(30);
            var builder = new ApplicationBuilder(serviceProvider: null)
                .UseRewriter(options);
            var app = builder.Build();
            app.Invoke(context).Wait();
            Assert.True(context.Response.Headers["location"].First().StartsWith("https"));
        }

        [Theory(Skip = "Adapting to TestServer")]
        public async Task PathMatchFunc_RewriteScheme()
        {
            var options = new UrlRewriteOptions().RewriteScheme();
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRewriter(options);

                    app.Run(context => context.Response.WriteAsync(context.Request.Path));
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("http://foo.com/bar");

            //Assert.True(response.RequestMessage.); 
        }


        private HttpContext CreateRequest(string basePath, string requestPath)
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.PathBase = new PathString(basePath);
            context.Request.Path = new PathString(requestPath);
            context.Request.Host = new HostString("example.com");
            return context;
        }
    }
}
