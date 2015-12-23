// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.StaticFiles
{
    public class DirectoryBrowserMiddlewareTests
    {
        [Fact]
        public async Task NullArguments()
        {
            Assert.Throws<ArgumentException>(() => StaticFilesTestServer.Create(
                app => app.UseDirectoryBrowser(options => { options.Formatter = null; }),
            services => services.AddDirectoryBrowser()));

            // No exception, default provided
            StaticFilesTestServer.Create(
                app => app.UseDirectoryBrowser(options => { options.FileProvider = null; }),
                services => services.AddDirectoryBrowser());

            // PathString(null) is OK.
            var server = StaticFilesTestServer.Create(
                app => app.UseDirectoryBrowser((string)null),
                services => services.AddDirectoryBrowser());

            var response = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("", @".", "/missing.dir")]
        [InlineData("", @".", "/missing.dir/")]
        [InlineData("/subdir", @".", "/subdir/missing.dir")]
        [InlineData("/subdir", @".", "/subdir/missing.dir/")]
        [InlineData("", @"./", "/missing.dir")]
        public async Task NoMatch_PassesThrough_All(string baseUrl, string baseDir, string requestUrl)
        {
            await NoMatch_PassesThrough(baseUrl, baseDir, requestUrl);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("", @".\", "/missing.dir")]
        [InlineData("", @".\", "/Missing.dir")]
        public async Task NoMatch_PassesThrough_Windows(string baseUrl, string baseDir, string requestUrl)
        {
            await NoMatch_PassesThrough(baseUrl, baseDir, requestUrl);
        }

        public async Task NoMatch_PassesThrough(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(
                    app => app.UseDirectoryBrowser(options =>
                    {
                        options.RequestPath = new PathString(baseUrl);
                        options.FileProvider = fileProvider;
                    }),
                    services => services.AddDirectoryBrowser());
                var response = await server.CreateRequest(requestUrl).GetAsync();
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("", @".", "/")]
        [InlineData("", @".", "/SubFolder/")]
        [InlineData("/somedir", @".", "/somedir/")]
        [InlineData("/somedir", @"./", "/somedir/")]
        [InlineData("/somedir", @".", "/somedir/SubFolder/")]
        public async Task FoundDirectory_Served_All(string baseUrl, string baseDir, string requestUrl)
        {
            await FoundDirectory_Served(baseUrl, baseDir, requestUrl);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("/somedir", @".\", "/somedir/")]
        [InlineData("/somedir", @".", "/somedir/subFolder/")]
        public async Task FoundDirectory_Served_Windows(string baseUrl, string baseDir, string requestUrl)
        {
            await FoundDirectory_Served(baseUrl, baseDir, requestUrl);
        }

        public async Task FoundDirectory_Served(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(
                    app => app.UseDirectoryBrowser(options =>
                    {
                        options.RequestPath = new PathString(baseUrl);
                        options.FileProvider = fileProvider;
                    }),
                    services => services.AddDirectoryBrowser());
                var response = await server.CreateRequest(requestUrl).GetAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
                Assert.True(response.Content.Headers.ContentLength > 0);
                Assert.Equal(response.Content.Headers.ContentLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Theory]
        [InlineData("", @".", "/SubFolder", "")]
        [InlineData("/somedir", @".", "/somedir", "")]
        [InlineData("/somedir", @".", "/somedir/SubFolder", "")]
        [InlineData("", @".", "/SubFolder", "?a=b")]
        [InlineData("/somedir", @".", "/somedir", "?a=b")]
        [InlineData("/somedir", @".", "/somedir/SubFolder", "?a=b")]
        public async Task NearMatch_RedirectAddSlash_All(string baseUrl, string baseDir, string requestUrl, string queryString)
        {
            await NearMatch_RedirectAddSlash(baseUrl, baseDir, requestUrl, queryString);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("/somedir", @".", "/somedir/subFolder", "")]
        [InlineData("/somedir", @".", "/somedir/subFolder", "?a=b")]
        public async Task NearMatch_RedirectAddSlash_Windows(string baseUrl, string baseDir, string requestUrl, string queryString)
        {
            await NearMatch_RedirectAddSlash(baseUrl, baseDir, requestUrl, queryString);
        }

        public async Task NearMatch_RedirectAddSlash(string baseUrl, string baseDir, string requestUrl, string queryString)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(
                    app => app.UseDirectoryBrowser(options =>
                    {
                        options.RequestPath = new PathString(baseUrl);
                        options.FileProvider = fileProvider;
                    }),
                    services => services.AddDirectoryBrowser());

                var response = await server.CreateRequest(requestUrl + queryString).GetAsync();

                Assert.Equal(HttpStatusCode.Moved, response.StatusCode);
                Assert.Equal(requestUrl + "/" + queryString, response.Headers.GetValues("Location").FirstOrDefault());
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Theory]
        [InlineData("", @".", "/")]
        [InlineData("", @".", "/SubFolder/")]
        [InlineData("/somedir", @".", "/somedir/")]
        [InlineData("/somedir", @".", "/somedir/SubFolder/")]
        public async Task PostDirectory_PassesThrough_All(string baseUrl, string baseDir, string requestUrl)
        {
            await PostDirectory_PassesThrough(baseUrl, baseDir, requestUrl);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("/somedir", @".", "/somedir/subFolder/")]
        public async Task PostDirectory_PassesThrough_Windows(string baseUrl, string baseDir, string requestUrl)
        {
            await PostDirectory_PassesThrough(baseUrl, baseDir, requestUrl);
        }

        public async Task PostDirectory_PassesThrough(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(
                    app => app.UseDirectoryBrowser(options =>
                    {
                        options.RequestPath = new PathString(baseUrl);
                        options.FileProvider = fileProvider;
                    }),
                    services => services.AddDirectoryBrowser());

                var response = await server.CreateRequest(requestUrl).PostAsync();
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("", @".", "/")]
        [InlineData("", @".", "/SubFolder/")]
        [InlineData("/somedir", @".", "/somedir/")]
        [InlineData("/somedir", @".", "/somedir/SubFolder/")]
        public async Task HeadDirectory_HeadersButNotBodyServed_All(string baseUrl, string baseDir, string requestUrl)
        {
            await HeadDirectory_HeadersButNotBodyServed(baseUrl, baseDir, requestUrl);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("/somedir", @".", "/somedir/subFolder/")]
        public async Task HeadDirectory_HeadersButNotBodyServed_Windows(string baseUrl, string baseDir, string requestUrl)
        {
            await HeadDirectory_HeadersButNotBodyServed(baseUrl, baseDir, requestUrl);
        }

        public async Task HeadDirectory_HeadersButNotBodyServed(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(
                    app => app.UseDirectoryBrowser(options =>
                    {
                        options.RequestPath = new PathString(baseUrl);
                        options.FileProvider = fileProvider;
                    }),
                    services => services.AddDirectoryBrowser());

                var response = await server.CreateRequest(requestUrl).SendAsync("HEAD");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
                Assert.True(response.Content.Headers.ContentLength == 0);
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }
    }
}
