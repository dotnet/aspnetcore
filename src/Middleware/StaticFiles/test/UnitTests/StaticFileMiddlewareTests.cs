// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Microsoft.AspNetCore.StaticFiles;

public class StaticFileMiddlewareTests : LoggedTest
{
    [Fact]
    public async Task ReturnsNotFoundWithoutWwwroot()
    {
        using var host = new HostBuilder()
            .ConfigureServices(AddTestLogging)
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app => app.UseStaticFiles());
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync("/ranges.txt");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Headers.ETag);

        Assert.Contains(TestSink.Writes, w => w.Message.Contains("The WebRootPath was not found")
            && w.Message.Contains("Static files may be unavailable."));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Symlinks not supported on Windows")]
    public async Task ReturnsNotFoundForBrokenSymlink()
    {
        var badLink = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName() + ".txt");

        Process.Start("ln", $"-s \"/tmp/{Path.GetRandomFileName()}\" \"{badLink}\"").WaitForExit();
        Assert.True(File.Exists(badLink), "Should have created a symlink");

        try
        {
            using var host = new HostBuilder()
            .ConfigureServices(AddTestLogging)
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app => app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true }))
                .UseWebRoot(AppContext.BaseDirectory);
            }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();

            var response = await server.CreateClient().GetAsync(Path.GetFileName(badLink));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null(response.Headers.ETag);
        }
        finally
        {
            File.Delete(badLink);
        }
    }

    [Fact]
    public async Task ReturnsNotFoundIfSendFileThrows()
    {
        var mockSendFile = new Mock<IHttpResponseBodyFeature>();
        mockSendFile.Setup(m => m.SendFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());
        mockSendFile.Setup(m => m.Stream).Returns(Stream.Null);
        using var host = new HostBuilder()
            .ConfigureServices(AddTestLogging)
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(async (ctx, next) =>
                    {
                        ctx.Features.Set(mockSendFile.Object);
                        await next(ctx);
                    });
                    app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });
                })
                .UseWebRoot(AppContext.BaseDirectory);
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync("TestDocument.txt");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Headers.ETag);
    }

    [Fact]
    public async Task FoundFile_LastModifiedTrimsSeconds()
    {
        using (var fileProvider = new PhysicalFileProvider(AppContext.BaseDirectory))
        {
            using var host = await StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider
            }));
            using var server = host.GetTestServer();
            var fileInfo = fileProvider.GetFileInfo("TestDocument.txt");
            var response = await server.CreateRequest("TestDocument.txt").GetAsync();

            var last = fileInfo.LastModified;
            var trimmed = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Offset).ToUniversalTime();

            Assert.Equal(response.Content.Headers.LastModified.Value, trimmed);
        }
    }

    [Fact]
    public async Task NullArguments()
    {
        // No exception, default provided
        using (await StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = null })))
        { }

        // No exception, default provided
        using (await StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions { FileProvider = null })))
        { }

        // PathString(null) is OK.
        using var host = await StaticFilesTestServer.Create(app => app.UseStaticFiles((string)null));
        using var server = host.GetTestServer();
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

    private async Task FoundFile_Served(string baseUrl, string baseDir, string requestUrl)
    {
        using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
        {
            using var host = await StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString(baseUrl),
                FileProvider = fileProvider
            }));
            using var server = host.GetTestServer();
            var fileInfo = fileProvider.GetFileInfo(Path.GetFileName(requestUrl));
            var response = await server.CreateRequest(requestUrl).GetAsync();
            var responseContent = await response.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
            Assert.Equal(response.Content.Headers.ContentLength, responseContent.Length);
            Assert.NotNull(response.Headers.ETag);

            using (var stream = fileInfo.CreateReadStream())
            {
                var fileContents = new byte[stream.Length];
                stream.Read(fileContents, 0, (int)stream.Length);
                Assert.True(responseContent.SequenceEqual(fileContents));
            }
        }
    }

    [Fact]
    public async Task OnPrepareResponse_Executed_Test()
    {
        var baseUrl = "";
        var baseDir = @".";
        var requestUrl = "/TestDocument.txt";

        var onPrepareResponseExecuted = false;

        using var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir));
        using var host = await StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = new PathString(baseUrl),
            FileProvider = fileProvider,
            OnPrepareResponse = context =>
            {
                onPrepareResponseExecuted = true;
            }
        }));
        using var server = host.GetTestServer();
        var fileInfo = fileProvider.GetFileInfo(Path.GetFileName(requestUrl));
        var response = await server.CreateRequest(requestUrl).GetAsync();
        var responseContent = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
        Assert.Equal(response.Content.Headers.ContentLength, responseContent.Length);
        Assert.NotNull(response.Headers.ETag);

        using (var stream = fileInfo.CreateReadStream())
        {
            var fileContents = new byte[stream.Length];
            stream.Read(fileContents, 0, (int)stream.Length);
            Assert.True(responseContent.SequenceEqual(fileContents));
        }

        Assert.True(onPrepareResponseExecuted);
    }

    [Fact]
    public async Task OnPrepareResponseAsync_Executed_Test()
    {
        var baseUrl = "";
        var baseDir = @".";
        var requestUrl = "/TestDocument.txt";

        var onPrepareResponseExecuted = false;

        using var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir));
        using var host = await StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = new PathString(baseUrl),
            FileProvider = fileProvider,
            OnPrepareResponseAsync = context =>
            {
                onPrepareResponseExecuted = true;

                return Task.CompletedTask;
            }
        }));
        using var server = host.GetTestServer();
        var fileInfo = fileProvider.GetFileInfo(Path.GetFileName(requestUrl));
        var response = await server.CreateRequest(requestUrl).GetAsync();
        var responseContent = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
        Assert.Equal(response.Content.Headers.ContentLength, responseContent.Length);
        Assert.NotNull(response.Headers.ETag);

        using (var stream = fileInfo.CreateReadStream())
        {
            var fileContents = new byte[stream.Length];
            stream.Read(fileContents, 0, (int)stream.Length);
            Assert.True(responseContent.SequenceEqual(fileContents));
        }

        Assert.True(onPrepareResponseExecuted);
    }

    [Fact]
    public async Task OnPrepareResponse_Execution_Order_Test()
    {
        var baseUrl = "";
        var baseDir = @".";
        var requestUrl = "/TestDocument.txt";

        var syncCallbackInvoked = false;
        var asyncCallbackInvoked = false;

        using var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir));
        using var host = await StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = new PathString(baseUrl),
            FileProvider = fileProvider,
            OnPrepareResponse = context =>
            {
                Assert.False(syncCallbackInvoked);
                Assert.False(asyncCallbackInvoked);
                syncCallbackInvoked = true;
            },
            OnPrepareResponseAsync = context =>
            {
                Assert.True(syncCallbackInvoked);
                Assert.False(asyncCallbackInvoked);
                asyncCallbackInvoked = true;
                return Task.CompletedTask;
            }
        }));
        using var server = host.GetTestServer();
        var fileInfo = fileProvider.GetFileInfo(Path.GetFileName(requestUrl));
        var response = await server.CreateRequest(requestUrl).GetAsync();
        var responseContent = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
        Assert.Equal(response.Content.Headers.ContentLength, responseContent.Length);
        Assert.NotNull(response.Headers.ETag);

        using (var stream = fileInfo.CreateReadStream())
        {
            var fileContents = new byte[stream.Length];
            stream.Read(fileContents, 0, (int)stream.Length);
            Assert.True(responseContent.SequenceEqual(fileContents));
        }

        Assert.True(syncCallbackInvoked);
        Assert.True(asyncCallbackInvoked);
    }

    [Fact]
    public async Task File_Served_If_Endpoint_With_Null_RequestDelegate_Is_Active()
    {
        using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, ".")))
        {
            using var host = await StaticFilesTestServer.Create(app =>
            {
                app.UseRouting();
                app.Use((ctx, next) =>
                {
                    ctx.SetEndpoint(new Endpoint(requestDelegate: null, new EndpointMetadataCollection(), "NullRequestDelegateEndpoint"));
                    return next();
                });
                app.UseStaticFiles(new StaticFileOptions
                {
                    RequestPath = new PathString(),
                    FileProvider = fileProvider
                });
                app.UseEndpoints(endpoints => { });
            }, services => services.AddRouting());
            using var server = host.GetTestServer();
            var requestUrl = "/TestDocument.txt";
            var fileInfo = fileProvider.GetFileInfo(Path.GetFileName(requestUrl));
            var response = await server.CreateRequest(requestUrl).GetAsync();
            var responseContent = await response.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
            Assert.Equal(response.Content.Headers.ContentLength, responseContent.Length);
            Assert.NotNull(response.Headers.ETag);

            using (var stream = fileInfo.CreateReadStream())
            {
                var fileContents = new byte[stream.Length];
                stream.Read(fileContents, 0, (int)stream.Length);
                Assert.True(responseContent.SequenceEqual(fileContents));
            }
        }
    }

    [Fact]
    public async Task File_NotServed_If_Endpoint_With_RequestDelegate_Is_Active()
    {
        var responseText = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        RequestDelegate handler = async (ctx) =>
        {
            ctx.Response.ContentType = "text/customfortest+plain";
            await ctx.Response.WriteAsync(responseText);
        };

        using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, ".")))
        {
            using var host = await StaticFilesTestServer.Create(app =>
            {
                app.UseRouting();
                app.Use((ctx, next) =>
                {
                    ctx.SetEndpoint(new Endpoint(handler, new EndpointMetadataCollection(), "RequestDelegateEndpoint"));
                    return next();
                });
                app.UseStaticFiles(new StaticFileOptions
                {
                    RequestPath = new PathString(),
                    FileProvider = fileProvider
                });
                app.UseEndpoints(endpoints => { });
            }, services => services.AddRouting());
            using var server = host.GetTestServer();
            var requestUrl = "/TestDocument.txt";

            var response = await server.CreateRequest(requestUrl).GetAsync();
            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/customfortest+plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal(responseText, responseContent);
        }
    }

    [Fact]
    public async Task OverrideDefaultStatusCode()
    {
        using var host = await StaticFilesTestServer.Create(app =>
        {
            app.Use(next => context => 
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return next(context);
            });
            app.UseStaticFiles();
        });

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <remarks>
    /// Note that the status code *might* be overridden if the static files middleware
    /// delegates to `next` (e.g. if the file isn't found and hits the 404 middleware).
    /// </remarks>
    [Fact]
    public async Task DontOverrideNonDefaultStatusCode()
    {
        const HttpStatusCode errorCode = HttpStatusCode.InsufficientStorage;

        using var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "."));

        using var host = await StaticFilesTestServer.Create(app =>
        {
            app.Use(next => context =>
            {
                context.Response.StatusCode = (int)errorCode;
                return next(context);
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString(),
                FileProvider = fileProvider
            });
        });

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("/TestDocument.txt");
        Assert.Equal(errorCode, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ExistingFiles))]
    public async Task HeadFile_HeadersButNotBodyServed(string baseUrl, string baseDir, string requestUrl)
    {
        using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
        {
            using var host = await StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString(baseUrl),
                FileProvider = fileProvider
            }));
            using var server = host.GetTestServer();
            var fileInfo = fileProvider.GetFileInfo(Path.GetFileName(requestUrl));
            var response = await server.CreateRequest(requestUrl).SendAsync("HEAD");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
            Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
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

    private async Task PassesThrough(string method, string baseUrl, string baseDir, string requestUrl)
    {
        using (var fileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, baseDir)))
        {
            using var host = await StaticFilesTestServer.Create(app => app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString(baseUrl),
                FileProvider = fileProvider
            }));
            using var server = host.GetTestServer();
            var response = await server.CreateRequest(requestUrl).SendAsync(method);
            Assert.Null(response.Content.Headers.LastModified);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    public static IEnumerable<object[]> MissingFiles => new[]
    {
            new[] {"", @".", "/missing.file"},
            new[] {"/subdir", @".", "/subdir/missing.file"},
            new[] {"/missing.file", @"./", "/missing.file"},
            new[] {"", @"./", "/xunit.xml"}
        };

    public static IEnumerable<object[]> ExistingFiles => new[]
    {
            new[] {"", @".", "/TestDocument.txt"},
            new[] {"/somedir", @".", "/somedir/TestDocument.txt"},
            new[] {"/SomeDir", @".", "/soMediR/TestDocument.txt"},
            new[] {"", @"SubFolder", "/ranges.txt"},
            new[] {"/somedir", @"SubFolder", "/somedir/ranges.txt"},
            new[] {"", @"SubFolder", "/Empty.txt"}
        };
}
