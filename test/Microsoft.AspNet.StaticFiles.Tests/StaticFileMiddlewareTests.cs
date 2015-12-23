// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Microsoft.AspNet.Testing.xunit;
using Xunit;

namespace Microsoft.AspNet.StaticFiles
{
    public class StaticFileMiddlewareTests
    {
        [Fact]
        public async Task ReturnsNotFoundWithoutWwwroot()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app => app.UseStaticFiles());
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("/ranges.txt");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task NullArguments()
        {
            Assert.Throws<ArgumentException>(() => StaticFilesTestServer.Create(app => app.UseStaticFiles(options => { options.ContentTypeProvider = null; })));

            // No exception, default provided
            StaticFilesTestServer.Create(app => app.UseStaticFiles(options => { options.FileProvider = null; }));

            // PathString(null) is OK.
            var server = StaticFilesTestServer.Create(app => app.UseStaticFiles((string)null));
            var response = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("", @".", "/missing.file")]
        [InlineData("/subdir", @".", "/subdir/missing.file")]
        [InlineData("/missing.file", @"./", "/missing.file")]
        [InlineData("", @"./", "/xunit.xml")]
        public async Task NoMatch_PassesThrough(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(app => app.UseStaticFiles(options =>
                {
                    options.RequestPath = new PathString(baseUrl);
                    options.FileProvider = fileProvider;
                }));
                var response = await server.CreateRequest(requestUrl).GetAsync();
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("", @".", "/TestDocument.txt")]
        [InlineData("/somedir", @".", "/somedir/TestDocument.txt")]
        [InlineData("/SomeDir", @".", "/soMediR/TestDocument.txt")]
        [InlineData("", @"SubFolder", "/ranges.txt")]
        [InlineData("/somedir", @"SubFolder", "/somedir/ranges.txt")]
        public async Task FoundFile_Served_All(string baseUrl, string baseDir, string requestUrl)
        {
            await FoundFile_Served(baseUrl, baseDir, requestUrl);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("", @".", "/testDocument.Txt")]
        [InlineData("/somedir", @".", "/somedir/Testdocument.TXT")]
        [InlineData("/SomeDir", @".", "/soMediR/testdocument.txT")]
        [InlineData("/somedir", @"SubFolder", "/somedir/Ranges.tXt")]
        public async Task FoundFile_Served_Windows(string baseUrl, string baseDir, string requestUrl)
        {
            await FoundFile_Served(baseUrl, baseDir, requestUrl);
        }

        public async Task FoundFile_Served(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(app => app.UseStaticFiles(options =>
                {
                    options.RequestPath = new PathString(baseUrl);
                    options.FileProvider = fileProvider;
                }));
                var response = await server.CreateRequest(requestUrl).GetAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
                Assert.True(response.Content.Headers.ContentLength > 0);
                Assert.Equal(response.Content.Headers.ContentLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Theory]
        [InlineData("", @".", "/TestDocument.txt")]
        [InlineData("/somedir", @".", "/somedir/TestDocument.txt")]
        [InlineData("/SomeDir", @".", "/soMediR/TestDocument.txt")]
        [InlineData("", @"SubFolder", "/ranges.txt")]
        [InlineData("/somedir", @"SubFolder", "/somedir/ranges.txt")]
        public async Task PostFile_PassesThrough(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(app => app.UseStaticFiles(options =>
                {
                    options.RequestPath = new PathString(baseUrl);
                    options.FileProvider = fileProvider;
                }));
                var response = await server.CreateRequest(requestUrl).PostAsync();
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("", @".", "/TestDocument.txt")]
        [InlineData("/somedir", @".", "/somedir/TestDocument.txt")]
        [InlineData("/SomeDir", @".", "/soMediR/TestDocument.txt")]
        [InlineData("", @"SubFolder", "/ranges.txt")]
        [InlineData("/somedir", @"SubFolder", "/somedir/ranges.txt")]
        public async Task HeadFile_HeadersButNotBodyServed(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(app => app.UseStaticFiles(options =>
                {
                    options.RequestPath = new PathString(baseUrl);
                    options.FileProvider = fileProvider;
                }));
                var response = await server.CreateRequest(requestUrl).SendAsync("HEAD");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
                Assert.True(response.Content.Headers.ContentLength > 0);
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }
    }
}
