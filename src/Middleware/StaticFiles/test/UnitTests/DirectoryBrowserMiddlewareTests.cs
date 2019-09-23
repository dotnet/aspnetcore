// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.AspNetCore.StaticFiles
{
    public class DirectoryBrowserMiddlewareTests
    {
        [Fact]
        public void WorksWithoutEncoderRegistered()
        {
            // No exception, uses HtmlEncoder.Default
            StaticFilesTestServer.Create(
                app => app.UseDirectoryBrowser());
        }

        [Fact]
        public async Task NullArguments()
        {
            // No exception, default provided
            StaticFilesTestServer.Create(
                app => app.UseDirectoryBrowser(new DirectoryBrowserOptions { Formatter = null }),
            services => services.AddDirectoryBrowser());

            // No exception, default provided
            StaticFilesTestServer.Create(
                app => app.UseDirectoryBrowser(new DirectoryBrowserOptions { FileProvider = null }),
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
        [InlineData("", @".", "/missing.dir", false)]
        [InlineData("", @".", "/missing.dir/", false)]
        [InlineData("/subdir", @".", "/subdir/missing.dir", false)]
        [InlineData("/subdir", @".", "/subdir/missing.dir/", false)]
        [InlineData("", @"./", "/missing.dir", false)]
        public async Task NoMatch_PassesThrough_All(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            await NoMatch_PassesThrough(baseUrl, baseDir, requestUrl, appendTrailingSlash);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("", @".\", "/missing.dir")]
        [InlineData("", @".\", "/Missing.dir")]
        [InlineData("", @".\", "/missing.dir", false)]
        [InlineData("", @".\", "/Missing.dir", false)]
        public async Task NoMatch_PassesThrough_Windows(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            await NoMatch_PassesThrough(baseUrl, baseDir, requestUrl, appendTrailingSlash);
        }

        private async Task NoMatch_PassesThrough(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
            {
                var server = StaticFilesTestServer.Create(
                    app => app.UseDirectoryBrowser(new DirectoryBrowserOptions
                    {
                        RequestPath = new PathString(baseUrl),
                        FileProvider = fileProvider,
                        RedirectToAppendTrailingSlash = appendTrailingSlash
                    }),
                    services => services.AddDirectoryBrowser());
                var response = await server.CreateRequest(requestUrl).GetAsync();
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task Endpoint_PassesThrough()
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, ".")))
            {
                var server = StaticFilesTestServer.Create(
                    app =>
                    {
                        app.UseRouting();

                        app.Use(next => context =>
                        {
                            // Assign an endpoint, this will make the directory browser noop
                            context.SetEndpoint(new Endpoint((c) =>
                            {
                                c.Response.StatusCode = (int)HttpStatusCode.NotAcceptable;
                                return c.Response.WriteAsync("Hi from endpoint.");
                            },
                            new EndpointMetadataCollection(),
                            "test"));

                            return next(context);
                        });

                        app.UseDirectoryBrowser(new DirectoryBrowserOptions
                        {
                            RequestPath = new PathString(""),
                            FileProvider = fileProvider
                        });

                        app.UseEndpoints(endpoints => { });
                    },
                    services => { services.AddDirectoryBrowser(); services.AddRouting(); });

                var response = await server.CreateRequest("/").GetAsync();
                Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
                Assert.Equal("Hi from endpoint.", await response.Content.ReadAsStringAsync());
            }
        }

        [Theory]
        [InlineData("", @".", "/")]
        [InlineData("", @".", "/SubFolder/")]
        [InlineData("/somedir", @".", "/somedir/")]
        [InlineData("/somedir", @"./", "/somedir/")]
        [InlineData("/somedir", @".", "/somedir/SubFolder/")]
        [InlineData("", @".", "/", false)]
        [InlineData("", @".", "/SubFolder/", false)]
        [InlineData("/somedir", @".", "/somedir/", false)]
        [InlineData("/somedir", @"./", "/somedir/", false)]
        [InlineData("/somedir", @".", "/somedir/SubFolder/", false)]
        [InlineData("", @".", "", false)]
        [InlineData("", @".", "/SubFolder", false)]
        [InlineData("/somedir", @".", "/somedir", false)]
        [InlineData("/somedir", @"./", "/somedir", false)]
        [InlineData("/somedir", @".", "/somedir/SubFolder", false)]
        public async Task FoundDirectory_Served_All(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            await FoundDirectory_Served(baseUrl, baseDir, requestUrl, appendTrailingSlash);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("/somedir", @".\", "/somedir/")]
        [InlineData("/somedir", @".", "/somedir/subFolder/")]
        [InlineData("/somedir", @".\", "/somedir/", false)]
        [InlineData("/somedir", @".", "/somedir/subFolder/", false)]
        [InlineData("/somedir", @".\", "/somedir", false)]
        [InlineData("/somedir", @".", "/somedir/subFolder", false)]
        public async Task FoundDirectory_Served_Windows(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            await FoundDirectory_Served(baseUrl, baseDir, requestUrl, appendTrailingSlash);
        }

        private async Task FoundDirectory_Served(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
            {
                var server = StaticFilesTestServer.Create(
                    app => app.UseDirectoryBrowser(new DirectoryBrowserOptions
                    {
                        RequestPath = new PathString(baseUrl),
                        FileProvider = fileProvider,
                        RedirectToAppendTrailingSlash = appendTrailingSlash,
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

        private async Task NearMatch_RedirectAddSlash(string baseUrl, string baseDir, string requestUrl, string queryString)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
            {
                var server = StaticFilesTestServer.Create(
                    app => app.UseDirectoryBrowser(new DirectoryBrowserOptions
                    {
                        RequestPath = new PathString(baseUrl),
                        FileProvider = fileProvider
                    }),
                    services => services.AddDirectoryBrowser());

                var response = await server.CreateRequest(requestUrl + queryString).GetAsync();

                Assert.Equal(HttpStatusCode.Moved, response.StatusCode);
                Assert.Equal("http://localhost" + requestUrl + "/" + queryString, response.Headers.GetValues("Location").FirstOrDefault());
                Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
            }
        }

        [Theory]
        [InlineData("", @".", "/")]
        [InlineData("", @".", "/SubFolder/")]
        [InlineData("/somedir", @".", "/somedir/")]
        [InlineData("/somedir", @".", "/somedir/SubFolder/")]
        [InlineData("", @".", "/", false)]
        [InlineData("", @".", "/SubFolder/", false)]
        [InlineData("/somedir", @".", "/somedir/", false)]
        [InlineData("/somedir", @".", "/somedir/SubFolder/", false)]
        [InlineData("", @".", "", false)]
        [InlineData("", @".", "/SubFolder", false)]
        [InlineData("/somedir", @".", "/somedir", false)]
        [InlineData("/somedir", @".", "/somedir/SubFolder", false)]
        public async Task PostDirectory_PassesThrough_All(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            await PostDirectory_PassesThrough(baseUrl, baseDir, requestUrl, appendTrailingSlash);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("/somedir", @".", "/somedir/subFolder/")]
        [InlineData("/somedir", @".", "/somedir/subFolder/", false)]
        [InlineData("/somedir", @".", "/somedir/subFolder", false)]
        public async Task PostDirectory_PassesThrough_Windows(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            await PostDirectory_PassesThrough(baseUrl, baseDir, requestUrl, appendTrailingSlash);
        }

        private async Task PostDirectory_PassesThrough(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
            {
                var server = StaticFilesTestServer.Create(
                    app => app.UseDirectoryBrowser(new DirectoryBrowserOptions
                    {
                        RequestPath = new PathString(baseUrl),
                        FileProvider = fileProvider,
                        RedirectToAppendTrailingSlash = appendTrailingSlash
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
        [InlineData("", @".", "/", false)]
        [InlineData("", @".", "/SubFolder/", false)]
        [InlineData("/somedir", @".", "/somedir/", false)]
        [InlineData("/somedir", @".", "/somedir/SubFolder/", false)]
        [InlineData("", @".", "", false)]
        [InlineData("", @".", "/SubFolder", false)]
        [InlineData("/somedir", @".", "/somedir", false)]
        [InlineData("/somedir", @".", "/somedir/SubFolder", false)]
        public async Task HeadDirectory_HeadersButNotBodyServed_All(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            await HeadDirectory_HeadersButNotBodyServed(baseUrl, baseDir, requestUrl, appendTrailingSlash);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("/somedir", @".", "/somedir/subFolder/")]
        [InlineData("/somedir", @".", "/somedir/subFolder/", false)]
        [InlineData("/somedir", @".", "/somedir/subFolder", false)]
        public async Task HeadDirectory_HeadersButNotBodyServed_Windows(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            await HeadDirectory_HeadersButNotBodyServed(baseUrl, baseDir, requestUrl, appendTrailingSlash);
        }

        private async Task HeadDirectory_HeadersButNotBodyServed(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
            {
                var server = StaticFilesTestServer.Create(
                    app => app.UseDirectoryBrowser(new DirectoryBrowserOptions
                    {
                        RequestPath = new PathString(baseUrl),
                        FileProvider = fileProvider,
                        RedirectToAppendTrailingSlash = appendTrailingSlash
                    }),
                    services => services.AddDirectoryBrowser());

                var response = await server.CreateRequest(requestUrl).SendAsync("HEAD");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
                Assert.Null(response.Content.Headers.ContentLength);
                Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
            }
        }

        [Fact]
        public void Options_AppendTrailingSlashByDefault()
        {
            Assert.True(new DirectoryBrowserOptions().RedirectToAppendTrailingSlash);
        }
    }
}
