// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.StaticFiles;

public class DefaultFilesMiddlewareTests
{
    [Fact]
    public async Task NullArguments()
    {
        // No exception, default provided
        using (await StaticFilesTestServer.Create(app => app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = null })))
        { }

        // PathString(null) is OK.
        using var host = await StaticFilesTestServer.Create(app => app.UseDefaultFiles((string)null));
        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            using var host = await StaticFilesTestServer.Create(app =>
            {
                app.UseDefaultFiles(new DefaultFilesOptions
                {
                    RequestPath = new PathString(baseUrl),
                    FileProvider = fileProvider,
                    RedirectToAppendTrailingSlash = appendTrailingSlash
                });
                app.Run(context => context.Response.WriteAsync(context.Request.Path.Value));
            });
            using var server = host.GetTestServer();

            var response = await server.CreateClient().GetAsync(requestUrl);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(requestUrl, await response.Content.ReadAsStringAsync()); // Should not be modified
        }
    }

    [Fact]
    public async Task Endpoint_With_RequestDelegate_PassesThrough()
    {
        using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, ".")))
        {
            using var host = await StaticFilesTestServer.Create(
                app =>
                {
                    app.UseRouting();

                    app.Use(next => context =>
                    {
                        // Assign an endpoint, this will make the default files noop.
                        context.SetEndpoint(new Endpoint((c) =>
                        {
                            return context.Response.WriteAsync(context.Request.Path.Value);
                        },
                        new EndpointMetadataCollection(),
                        "test"));

                        return next(context);
                    });

                    app.UseDefaultFiles(new DefaultFilesOptions
                    {
                        RequestPath = new PathString(""),
                        FileProvider = fileProvider
                    });

                    app.UseEndpoints(endpoints => { });

                    // Echo back the current request path value
                    app.Run(context => context.Response.WriteAsync(context.Request.Path.Value));
                },
                services => { services.AddDirectoryBrowser(); services.AddRouting(); });
            using var server = host.GetTestServer();

            var response = await server.CreateRequest("/SubFolder/").GetAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/SubFolder/", await response.Content.ReadAsStringAsync()); // Should not be modified
        }
    }

    [Fact]
    public async Task Endpoint_With_Null_RequestDelegate_Does_Not_PassThrough()
    {
        using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, ".")))
        {
            using var host = await StaticFilesTestServer.Create(
                app =>
                {
                    app.UseRouting();

                    app.Use(next => context =>
                    {
                        // Assign an endpoint with a null RequestDelegate, the default files should still run
                        context.SetEndpoint(new Endpoint(requestDelegate: null,
                        new EndpointMetadataCollection(),
                        "test"));

                        return next(context);
                    });

                    app.UseDefaultFiles(new DefaultFilesOptions
                    {
                        RequestPath = new PathString(""),
                        FileProvider = fileProvider
                    });

                    app.UseEndpoints(endpoints => { });

                    // Echo back the current request path value
                    app.Run(context => context.Response.WriteAsync(context.Request.Path.Value));
                },
                services => { services.AddDirectoryBrowser(); services.AddRouting(); });
            using var server = host.GetTestServer();

            var response = await server.CreateRequest("/SubFolder/").GetAsync();
            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/SubFolder/default.html", responseContent); // Should be modified and be valid path to file
        }
    }

    [Theory]
    [InlineData("", @".", "/SubFolder/")]
    [InlineData("", @"./", "/SubFolder/")]
    [InlineData("", @"./SubFolder", "/")]
    [InlineData("", @"./SubFolder", "/你好/")]
    [InlineData("", @"./SubFolder", "/你好/世界/")]
    [InlineData("", @".", "/SubFolder/", false)]
    [InlineData("", @"./", "/SubFolder/", false)]
    [InlineData("", @"./SubFolder", "/", false)]
    [InlineData("", @"./SubFolder", "/你好/", false)]
    [InlineData("", @"./SubFolder", "/你好/世界/", false)]
    [InlineData("", @".", "/SubFolder", false)]
    [InlineData("", @"./", "/SubFolder", false)]
    [InlineData("", @"./SubFolder", "", false)]
    [InlineData("", @"./SubFolder", "/你好", false)]
    [InlineData("", @"./SubFolder", "/你好/世界", false)]
    public async Task FoundDirectoryWithDefaultFile_PathModified_All(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
    {
        await FoundDirectoryWithDefaultFile_PathModified(baseUrl, baseDir, requestUrl, appendTrailingSlash);
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    [InlineData("", @".\", "/SubFolder/")]
    [InlineData("", @".\subFolder", "/")]
    [InlineData("", @".\SubFolder", "/你好/")]
    [InlineData("", @".\SubFolder", "/你好/世界/")]
    [InlineData("", @".\", "/SubFolder/", false)]
    [InlineData("", @".\subFolder", "/", false)]
    [InlineData("", @".\SubFolder", "/你好/", false)]
    [InlineData("", @".\SubFolder", "/你好/世界/", false)]
    [InlineData("", @".\", "/SubFolder", false)]
    [InlineData("", @".\subFolder", "", false)]
    [InlineData("", @".\SubFolder", "/你好", false)]
    [InlineData("", @".\SubFolder", "/你好/世界", false)]
    public async Task FoundDirectoryWithDefaultFile_PathModified_Windows(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
    {
        await FoundDirectoryWithDefaultFile_PathModified(baseUrl, baseDir, requestUrl, appendTrailingSlash);
    }

    private async Task FoundDirectoryWithDefaultFile_PathModified(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
    {
        using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
        {
            using var host = await StaticFilesTestServer.Create(app =>
            {
                app.UseDefaultFiles(new DefaultFilesOptions
                {
                    RequestPath = new PathString(baseUrl),
                    FileProvider = fileProvider,
                    RedirectToAppendTrailingSlash = appendTrailingSlash
                });
                app.Run(context => context.Response.WriteAsync(context.Request.Path.Value));
            });
            using var server = host.GetTestServer();

            var response = await server.CreateClient().GetAsync(requestUrl);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var requestUrlWithSlash = requestUrl.EndsWith('/') ? requestUrl : requestUrl + "/";
            Assert.Equal(requestUrlWithSlash + "default.html", await response.Content.ReadAsStringAsync()); // Should be modified and be valid path to file
        }
    }

    [Theory]
    [InlineData("", @".", "/SubFolder", "")]
    [InlineData("", @"./", "/SubFolder", "")]
    [InlineData("", @"./", "/SubFolder", "?a=b")]
    [InlineData("", @"./SubFolder", "/你好", "?a=b")]
    [InlineData("", @"./SubFolder", "/你好/世界", "?a=b")]
    public async Task NearMatch_RedirectAddSlash_All(string baseUrl, string baseDir, string requestUrl, string queryString)
    {
        await NearMatch_RedirectAddSlash(baseUrl, baseDir, requestUrl, queryString);
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    [InlineData("", @".\", "/SubFolder", "")]
    [InlineData("", @".\", "/SubFolder", "?a=b")]
    [InlineData("", @".\SubFolder", "/你好", "?a=b")]
    [InlineData("", @".\SubFolder", "/你好/世界", "?a=b")]
    public async Task NearMatch_RedirectAddSlash_Windows(string baseUrl, string baseDir, string requestUrl, string queryString)
    {
        await NearMatch_RedirectAddSlash(baseUrl, baseDir, requestUrl, queryString);
    }

    private async Task NearMatch_RedirectAddSlash(string baseUrl, string baseDir, string requestUrl, string queryString)
    {
        using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
        {
            using var host = await StaticFilesTestServer.Create(app => app.UseDefaultFiles(new DefaultFilesOptions
            {
                RequestPath = new PathString(baseUrl),
                FileProvider = fileProvider
            }));
            using var server = host.GetTestServer();
            var response = await server.CreateRequest(requestUrl + queryString).GetAsync();

            Assert.Equal(HttpStatusCode.Moved, response.StatusCode);
            // the url in the header of `Location: /xxx/xxx` should be encoded
            var actualURL = response.Headers.GetValues("Location").FirstOrDefault();
            Assert.Equal("http://localhost" + baseUrl + new PathString(requestUrl + "/") + queryString, actualURL);
            Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
        }
    }

    [Theory]
    [InlineData("/SubFolder", @"./", "/SubFolder/")]
    [InlineData("/SubFolder", @".", "/somedir/")]
    [InlineData("", @"./SubFolder", "/")]
    [InlineData("", @"./SubFolder/", "/")]
    [InlineData("/SubFolder", @"./", "/SubFolder/", false)]
    [InlineData("/SubFolder", @".", "/somedir/", false)]
    [InlineData("", @"./SubFolder", "/", false)]
    [InlineData("", @"./SubFolder/", "/", false)]
    [InlineData("/SubFolder", @"./", "/SubFolder", false)]
    [InlineData("/SubFolder", @".", "/somedir", false)]
    [InlineData("", @"./SubFolder", "", false)]
    [InlineData("", @"./SubFolder/", "", false)]
    public async Task PostDirectory_PassesThrough_All(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
    {
        await PostDirectory_PassesThrough(baseUrl, baseDir, requestUrl, appendTrailingSlash);
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    [InlineData("/SubFolder", @".\", "/SubFolder/")]
    [InlineData("", @".\SubFolder", "/")]
    [InlineData("", @".\SubFolder\", "/")]
    [InlineData("/SubFolder", @".\", "/SubFolder/", false)]
    [InlineData("", @".\SubFolder", "/", false)]
    [InlineData("", @".\SubFolder\", "/", false)]
    [InlineData("/SubFolder", @".\", "/SubFolder", false)]
    [InlineData("", @".\SubFolder", "", false)]
    [InlineData("", @".\SubFolder\", "", false)]
    public async Task PostDirectory_PassesThrough_Windows(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
    {
        await PostDirectory_PassesThrough(baseUrl, baseDir, requestUrl, appendTrailingSlash);
    }

    private async Task PostDirectory_PassesThrough(string baseUrl, string baseDir, string requestUrl, bool appendTrailingSlash = true)
    {
        using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
        {
            using var host = await StaticFilesTestServer.Create(app => app.UseDefaultFiles(new DefaultFilesOptions
            {
                RequestPath = new PathString(baseUrl),
                FileProvider = fileProvider,
                RedirectToAppendTrailingSlash = appendTrailingSlash
            }));
            using var server = host.GetTestServer();
            var response = await server.CreateRequest(requestUrl).GetAsync();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // Passed through
        }
    }

    [Fact]
    public void Options_AppendTrailingSlashByDefault()
    {
        Assert.True(new DefaultFilesOptions().RedirectToAppendTrailingSlash);
    }
}
