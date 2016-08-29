// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.AspNetCore.StaticFiles
{
    public class StaticFileMiddlewareTests
    {
        [Fact]
        public async Task ReturnsNotFoundWithoutWwwroot()
        {
            var builder = new WebHostBuilder()
                .Configure(app => app.UseStaticFiles());
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("/ranges.txt");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FoundFile_LastModifiedTrimsSeconds()
        {
            using (var fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory()))
            {
                var server = StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = fileProvider
                }));
                var fileInfo = fileProvider.GetFileInfo("TestDocument.txt");
                var response = await server.CreateRequest("TestDocument.txt").GetAsync();

                var last = fileInfo.LastModified;
                var trimed = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Offset).ToUniversalTime();

                Assert.Equal(response.Content.Headers.LastModified.Value, trimed);
            }
        }

        [Fact]
        public async Task NullArguments()
        {
            // No exception, default provided
            StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = null }));

            // No exception, default provided
            StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions { FileProvider = null }));

            // PathString(null) is OK.
            var server = StaticFilesTestServer.Create(app => app.UseStaticFiles((string)null));
            var response = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(ExistingFiles))]
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
                var server = StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
                {
                    RequestPath = new PathString(baseUrl),
                    FileProvider = fileProvider
                }));
                var fileInfo = fileProvider.GetFileInfo(Path.GetFileName(requestUrl));
                var response = await server.CreateRequest(requestUrl).GetAsync();
                var responseContent = await response.Content.ReadAsByteArrayAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
                Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
                Assert.Equal(response.Content.Headers.ContentLength, responseContent.Length);

                using (var stream = fileInfo.CreateReadStream())
                {
                    var fileContents = new byte[stream.Length];
                    stream.Read(fileContents, 0, (int)stream.Length);
                    Assert.True(responseContent.SequenceEqual(fileContents));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ExistingFiles))]
        public async Task HeadFile_HeadersButNotBodyServed(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
                {
                    RequestPath = new PathString(baseUrl),
                    FileProvider = fileProvider
                }));
                var fileInfo = fileProvider.GetFileInfo(Path.GetFileName(requestUrl));
                var response = await server.CreateRequest(requestUrl).SendAsync("HEAD");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
                Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Theory]
        [MemberData(nameof(MissingFiles))]
        public async Task Get_NoMatch_PassesThrough(string baseUrl, string baseDir, string requestUrl) =>
            await PassesThrough("GET", baseUrl, baseDir, requestUrl);

        [Theory]
        [MemberData(nameof(MissingFiles))]
        public async Task Head_NoMatch_PassesThrough(string baseUrl, string baseDir, string requestUrl) =>
            await PassesThrough("HEAD", baseUrl, baseDir, requestUrl);

        [Theory]
        [MemberData(nameof(MissingFiles))]
        public async Task Unknown_NoMatch_PassesThrough(string baseUrl, string baseDir, string requestUrl) =>
            await PassesThrough("VERB", baseUrl, baseDir, requestUrl);

        [Theory]
        [MemberData(nameof(ExistingFiles))]
        public async Task Options_Match_PassesThrough(string baseUrl, string baseDir, string requestUrl) =>
            await PassesThrough("OPTIONS", baseUrl, baseDir, requestUrl);

        [Theory]
        [MemberData(nameof(ExistingFiles))]
        public async Task Trace_Match_PassesThrough(string baseUrl, string baseDir, string requestUrl) =>
            await PassesThrough("TRACE", baseUrl, baseDir, requestUrl);

        [Theory]
        [MemberData(nameof(ExistingFiles))]
        public async Task Post_Match_PassesThrough(string baseUrl, string baseDir, string requestUrl) =>
            await PassesThrough("POST", baseUrl, baseDir, requestUrl);

        [Theory]
        [MemberData(nameof(ExistingFiles))]
        public async Task Put_Match_PassesThrough(string baseUrl, string baseDir, string requestUrl) =>
            await PassesThrough("PUT", baseUrl, baseDir, requestUrl);

        [Theory]
        [MemberData(nameof(ExistingFiles))]
        public async Task Unknown_Match_PassesThrough(string baseUrl, string baseDir, string requestUrl) =>
            await PassesThrough("VERB", baseUrl, baseDir, requestUrl);

        public async Task PassesThrough(string method, string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
                {
                    RequestPath = new PathString(baseUrl),
                    FileProvider = fileProvider
                }));
                var response = await server.CreateRequest(requestUrl).SendAsync(method);
                Assert.Null(response.Content.Headers.LastModified);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        public static IEnumerable<string[]> MissingFiles => new[]
        {
            new[] {"", @".", "/missing.file"},
            new[] {"/subdir", @".", "/subdir/missing.file"},
            new[] {"/missing.file", @"./", "/missing.file"},
            new[] {"", @"./", "/xunit.xml"}
        };

        public static IEnumerable<string[]> ExistingFiles => new[]
        {
            new[] {"", @".", "/TestDocument.txt"},
            new[] {"/somedir", @".", "/somedir/TestDocument.txt"},
            new[] {"/SomeDir", @".", "/soMediR/TestDocument.txt"},
            new[] {"", @"SubFolder", "/ranges.txt"},
            new[] {"/somedir", @"SubFolder", "/somedir/ranges.txt"},
            new[] {"", @"SubFolder", "/Empty.txt"}
        };
    }
}
