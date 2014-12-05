// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.StaticFiles
{
    public class StaticFileMiddlewareTests
    {
        [Fact]
        public async Task NullArguments()
        {
            Assert.Throws<TargetInvocationException>(() => TestServer.Create(app => app.UseStaticFiles(new StaticFileOptions() { ContentTypeProvider = null })));

            // No exception, default provided
            TestServer.Create(app => app.UseStaticFiles(new StaticFileOptions() { FileSystem = null }));

            // PathString(null) is OK.
            TestServer server = TestServer.Create(app => app.UseStaticFiles((string)null));
            var response = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("", @".", "/missing.file")]
        [InlineData("/subdir", @".", "/subdir/missing.file")]
        [InlineData("/missing.file", @".\", "/missing.file")]
        [InlineData("", @".\", "/xunit.xml")]
        public async Task NoMatch_PassesThrough(string baseUrl, string baseDir, string requestUrl)
        {
            TestServer server = TestServer.Create(app => app.UseStaticFiles(new StaticFileOptions()
            {
                RequestPath = new PathString(baseUrl),
                FileSystem = new PhysicalFileSystem(Path.Combine(Environment.CurrentDirectory, baseDir))
            }));
            HttpResponseMessage response = await server.CreateRequest(requestUrl).GetAsync();
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("", @".", "/TestDocument.txt")]
        [InlineData("", @".", "/testDocument.Txt")]
        [InlineData("/somedir", @".", "/somedir/Testdocument.TXT")]
        [InlineData("/SomeDir", @".", "/soMediR/testdocument.txT")]
        [InlineData("", @"SubFolder", "/ranges.txt")]
        [InlineData("/somedir", @"SubFolder", "/somedir/Ranges.tXt")]
        public async Task FoundFile_Served(string baseUrl, string baseDir, string requestUrl)
        {
            TestServer server = TestServer.Create(app => app.UseStaticFiles(new StaticFileOptions()
            {
                RequestPath = new PathString(baseUrl),
                FileSystem = new PhysicalFileSystem(Path.Combine(Environment.CurrentDirectory, baseDir))
            }));
            HttpResponseMessage response = await server.CreateRequest(requestUrl).GetAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.True(response.Content.Headers.ContentLength > 0);
            Assert.Equal(response.Content.Headers.ContentLength, (await response.Content.ReadAsByteArrayAsync()).Length);
        }

        [Theory]
        [InlineData("", @".", "/TestDocument.txt")]
        [InlineData("", @".", "/testDocument.Txt")]
        [InlineData("/somedir", @".", "/somedir/Testdocument.TXT")]
        [InlineData("/SomeDir", @".", "/soMediR/testdocument.txT")]
        [InlineData("", @"SubFolder", "/ranges.txt")]
        [InlineData("/somedir", @"SubFolder", "/somedir/Ranges.tXt")]
        public async Task PostFile_PassesThrough(string baseUrl, string baseDir, string requestUrl)
        {
            TestServer server = TestServer.Create(app => app.UseStaticFiles(new StaticFileOptions()
            {
                RequestPath = new PathString(baseUrl),
                FileSystem = new PhysicalFileSystem(Path.Combine(Environment.CurrentDirectory, baseDir))
            }));
            HttpResponseMessage response = await server.CreateRequest(requestUrl).PostAsync();
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("", @".", "/TestDocument.txt")]
        [InlineData("", @".", "/testDocument.Txt")]
        [InlineData("/somedir", @".", "/somedir/Testdocument.TXT")]
        [InlineData("/SomeDir", @".", "/soMediR/testdocument.txT")]
        [InlineData("", @"SubFolder", "/ranges.txt")]
        [InlineData("/somedir", @"SubFolder", "/somedir/Ranges.tXt")]
        public async Task HeadFile_HeadersButNotBodyServed(string baseUrl, string baseDir, string requestUrl)
        {
            TestServer server = TestServer.Create(app => app.UseStaticFiles(new StaticFileOptions()
            {
                RequestPath = new PathString(baseUrl),
                FileSystem = new PhysicalFileSystem(Path.Combine(Environment.CurrentDirectory, baseDir))
            }));
            HttpResponseMessage response = await server.CreateRequest(requestUrl).SendAsync("HEAD");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.True(response.Content.Headers.ContentLength > 0);
            Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
        }
    }
}
