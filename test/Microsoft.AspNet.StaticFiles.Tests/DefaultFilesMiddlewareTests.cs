// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
using Xunit;

namespace Microsoft.AspNet.StaticFiles
{
    public class DefaultFilesMiddlewareTests
    {
        [Fact]
        public async Task NullArguments()
        {
            // No exception, default provided
            StaticFilesTestServer.Create(app => app.UseDefaultFiles(options => { options.FileProvider = null; }));

            // PathString(null) is OK.
            var server = StaticFilesTestServer.Create(app => app.UseDefaultFiles((string)null));
            var response = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
                var server = StaticFilesTestServer.Create(app =>
                {
                    app.UseDefaultFiles(options =>
                    {
                        options.RequestPath = new PathString(baseUrl);
                        options.FileProvider = fileProvider;
                    });
                    app.Run(context => context.Response.WriteAsync(context.Request.Path.Value));
                });

                var response = await server.CreateClient().GetAsync(requestUrl);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(requestUrl, await response.Content.ReadAsStringAsync()); // Should not be modified
            }
        }

        [Theory]
        [InlineData("", @".", "/SubFolder/")]
        [InlineData("", @"./", "/SubFolder/")]
        [InlineData("", @"./SubFolder", "/")]
        public async Task FoundDirectoryWithDefaultFile_PathModified_All(string baseUrl, string baseDir, string requestUrl)
        {
            await FoundDirectoryWithDefaultFile_PathModified(baseUrl, baseDir, requestUrl);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("", @".\", "/SubFolder/")]
        [InlineData("", @".\subFolder", "/")]
        public async Task FoundDirectoryWithDefaultFile_PathModified_Windows(string baseUrl, string baseDir, string requestUrl)
        {
            await FoundDirectoryWithDefaultFile_PathModified(baseUrl, baseDir, requestUrl);
        }

        public async Task FoundDirectoryWithDefaultFile_PathModified(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(app =>
                {
                    app.UseDefaultFiles(options =>
                    {
                        options.RequestPath = new PathString(baseUrl);
                        options.FileProvider = fileProvider;
                    });
                    app.Run(context => context.Response.WriteAsync(context.Request.Path.Value));
                });

                var response = await server.CreateClient().GetAsync(requestUrl);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(requestUrl + "default.html", await response.Content.ReadAsStringAsync()); // Should be modified
            }
        }

        [Theory]
        [InlineData("", @".", "/SubFolder", "")]
        [InlineData("", @"./", "/SubFolder", "")]
        [InlineData("", @"./", "/SubFolder", "?a=b")]
        public async Task NearMatch_RedirectAddSlash_All(string baseUrl, string baseDir, string requestUrl, string queryString)
        {
            await NearMatch_RedirectAddSlash(baseUrl, baseDir, requestUrl, queryString);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("", @".\", "/SubFolder", "")]
        [InlineData("", @".\", "/SubFolder", "?a=b")]
        public async Task NearMatch_RedirectAddSlash_Windows(string baseUrl, string baseDir, string requestUrl, string queryString)
        {
            await NearMatch_RedirectAddSlash(baseUrl, baseDir, requestUrl, queryString);
        }

        public async Task NearMatch_RedirectAddSlash(string baseUrl, string baseDir, string requestUrl, string queryString)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(app => app.UseDefaultFiles(options =>
                {
                    options.RequestPath = new PathString(baseUrl);
                    options.FileProvider = fileProvider;
                }));
                var response = await server.CreateRequest(requestUrl + queryString).GetAsync();

                Assert.Equal(HttpStatusCode.Moved, response.StatusCode);
                Assert.Equal(requestUrl + "/" + queryString, response.Headers.GetValues("Location").FirstOrDefault());
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Theory]
        [InlineData("/SubFolder", @"./", "/SubFolder/")]
        [InlineData("/SubFolder", @".", "/somedir/")]
        [InlineData("", @"./SubFolder", "/")]
        [InlineData("", @"./SubFolder/", "/")]
        public async Task PostDirectory_PassesThrough_All(string baseUrl, string baseDir, string requestUrl)
        {
            await PostDirectory_PassesThrough(baseUrl, baseDir, requestUrl);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("/SubFolder", @".\", "/SubFolder/")]
        [InlineData("", @".\SubFolder", "/")]
        [InlineData("", @".\SubFolder\", "/")]
        public async Task PostDirectory_PassesThrough_Windows(string baseUrl, string baseDir, string requestUrl)
        {
            await PostDirectory_PassesThrough(baseUrl, baseDir, requestUrl);
        }

        public async Task PostDirectory_PassesThrough(string baseUrl, string baseDir, string requestUrl)
        {
            using (var fileProvder = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), baseDir)))
            {
                var server = StaticFilesTestServer.Create(app => app.UseDefaultFiles(options =>
                {
                    options.RequestPath = new PathString(baseUrl);
                    options.FileProvider = fileProvder;
                }));
                var response = await server.CreateRequest(requestUrl).GetAsync();

                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // Passed through
            }
        }
    }
}
