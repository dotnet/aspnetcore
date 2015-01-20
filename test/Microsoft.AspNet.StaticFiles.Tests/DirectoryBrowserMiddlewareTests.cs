// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.StaticFiles
{
    public class DirectoryBrowserMiddlewareTests
    {
        [Fact]
        public async Task NullArguments()
        {
            Assert.Throws<ArgumentException>(() => TestServer.Create(app => app.UseDirectoryBrowser(new DirectoryBrowserOptions() { Formatter = null })));

            // No exception, default provided
            TestServer.Create(app => app.UseDirectoryBrowser(new DirectoryBrowserOptions() { FileProvider = null }));

            // PathString(null) is OK.
            TestServer server = TestServer.Create(app => app.UseDirectoryBrowser((string)null));
            var response = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("", @".", "/missing.dir")]
        [InlineData("", @".", "/missing.dir/")]
        [InlineData("/subdir", @".", "/subdir/missing.dir")]
        [InlineData("/subdir", @".", "/subdir/missing.dir/")]
        [InlineData("", @".\", "/missing.dir")]
        public async Task NoMatch_PassesThrough(string baseUrl, string baseDir, string requestUrl)
        {
            TestServer server = TestServer.Create(app => app.UseDirectoryBrowser(new DirectoryBrowserOptions()
            {
                RequestPath = new PathString(baseUrl),
                FileProvider = new PhysicalFileProvider(Path.Combine(Environment.CurrentDirectory, baseDir))
            }));
            HttpResponseMessage response = await server.CreateRequest(requestUrl).GetAsync();
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("", @".", "/")]
        [InlineData("", @".", "/SubFolder/")]
        [InlineData("/somedir", @".", "/somedir/")]
        [InlineData("/somedir", @".\", "/somedir/")]
        [InlineData("/somedir", @".", "/somedir/subfolder/")]
        public async Task FoundDirectory_Served(string baseUrl, string baseDir, string requestUrl)
        {
            TestServer server = TestServer.Create(app => app.UseDirectoryBrowser(new DirectoryBrowserOptions()
            {
                RequestPath = new PathString(baseUrl),
                FileProvider = new PhysicalFileProvider(Path.Combine(Environment.CurrentDirectory, baseDir))
            }));
            HttpResponseMessage response = await server.CreateRequest(requestUrl).GetAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
            Assert.True(response.Content.Headers.ContentLength > 0);
            Assert.Equal(response.Content.Headers.ContentLength, (await response.Content.ReadAsByteArrayAsync()).Length);
        }

        [Theory]
        [InlineData("", @".", "/SubFolder", "")]
        [InlineData("/somedir", @".", "/somedir", "")]
        [InlineData("/somedir", @".", "/somedir/subfolder", "")]
        [InlineData("", @".", "/SubFolder", "?a=b")]
        [InlineData("/somedir", @".", "/somedir", "?a=b")]
        [InlineData("/somedir", @".", "/somedir/subfolder", "?a=b")]
        public async Task NearMatch_RedirectAddSlash(string baseUrl, string baseDir, string requestUrl, string queryString)
        {
            TestServer server = TestServer.Create(app => app.UseDirectoryBrowser(new DirectoryBrowserOptions()
            {
                RequestPath = new PathString(baseUrl),
                FileProvider = new PhysicalFileProvider(Path.Combine(Environment.CurrentDirectory, baseDir))
            }));
            HttpResponseMessage response = await server.CreateRequest(requestUrl + queryString).GetAsync();

            Assert.Equal(HttpStatusCode.Moved, response.StatusCode);
            Assert.Equal(requestUrl + "/" + queryString, response.Headers.Location.ToString());
            Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
        }

        [Theory]
        [InlineData("", @".", "/")]
        [InlineData("", @".", "/SubFolder/")]
        [InlineData("/somedir", @".", "/somedir/")]
        [InlineData("/somedir", @".", "/somedir/subfolder/")]
        public async Task PostDirectory_PassesThrough(string baseUrl, string baseDir, string requestUrl)
        {
            TestServer server = TestServer.Create(app => app.UseDirectoryBrowser(new DirectoryBrowserOptions()
            {
                RequestPath = new PathString(baseUrl),
                FileProvider = new PhysicalFileProvider(Path.Combine(Environment.CurrentDirectory, baseDir))
            }));
            HttpResponseMessage response = await server.CreateRequest(requestUrl).PostAsync();
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("", @".", "/")]
        [InlineData("", @".", "/SubFolder/")]
        [InlineData("/somedir", @".", "/somedir/")]
        [InlineData("/somedir", @".", "/somedir/subfolder/")]
        public async Task HeadDirectory_HeadersButNotBodyServed(string baseUrl, string baseDir, string requestUrl)
        {
            TestServer server = TestServer.Create(app => app.UseDirectoryBrowser(new DirectoryBrowserOptions()
            {
                RequestPath = new PathString(baseUrl),
                FileProvider = new PhysicalFileProvider(Path.Combine(Environment.CurrentDirectory, baseDir))
            }));
            HttpResponseMessage response = await server.CreateRequest(requestUrl).SendAsync("HEAD");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
            Assert.True(response.Content.Headers.ContentLength == 0);
            Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
        }
    }
}
