// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.StaticFiles;

public class StaticFileMiddlewareTests : LoggedTest
{
    [Fact]
    public async Task ReturnsNotFoundWithoutWwwroot()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureServices(services => services.AddSingleton(LoggerFactory))
                .UseKestrel()
                .UseUrls(TestUrlHelper.GetTestUrl(ServerType.Kestrel))
                .Configure(app => app.UseStaticFiles());
            }).Build();

        await host.StartAsync();

        using (var client = new HttpClient { BaseAddress = new Uri(Helpers.GetAddress(host)) })
        {
            var response = await client.GetAsync("TestDocument.txt");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task Endpoint_PassesThrough()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureServices(services => { services.AddSingleton(LoggerFactory); services.AddRouting(); })
                .UseKestrel()
                .UseUrls(TestUrlHelper.GetTestUrl(ServerType.Kestrel))
                .UseWebRoot(AppContext.BaseDirectory)
                .Configure(app =>
                {
                    // Routing first => static files noops
                    app.UseRouting();

                    app.Use(next => context =>
                    {
                        // Assign an endpoint, this will make the default files noop.
                        context.SetEndpoint(new Endpoint((c) =>
                        {
                            return context.Response.WriteAsync("Hi from endpoint.");
                        },
                        new EndpointMetadataCollection(),
                        "test"));

                        return next(context);
                    });

                    app.UseStaticFiles();

                    app.UseEndpoints(endpoints => { });
                });
            }).Build();

        await host.StartAsync();

        using (var client = new HttpClient { BaseAddress = new Uri(Helpers.GetAddress(host)) })
        {
            var response = await client.GetAsync("TestDocument.txt");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hi from endpoint.", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task FoundFile_LastModifiedTrimsSeconds()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureServices(services => services.AddSingleton(LoggerFactory))
                .UseKestrel()
                .UseUrls(TestUrlHelper.GetTestUrl(ServerType.Kestrel))
                .UseWebRoot(AppContext.BaseDirectory)
                .Configure(app => app.UseStaticFiles());
            }).Build();

        await host.StartAsync();

        using (var client = new HttpClient { BaseAddress = new Uri(Helpers.GetAddress(host)) })
        {
            var last = File.GetLastWriteTimeUtc(Path.Combine(AppContext.BaseDirectory, "TestDocument.txt"));
            var response = await client.GetAsync("TestDocument.txt");

            var trimmed = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, TimeSpan.Zero).ToUniversalTime();

            Assert.Equal(response.Content.Headers.LastModified.Value, trimmed);
        }
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
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureServices(services => services.AddSingleton(LoggerFactory))
                .UseKestrel()
                .UseUrls(TestUrlHelper.GetTestUrl(ServerType.Kestrel))
                .UseWebRoot(Path.Combine(AppContext.BaseDirectory, baseDir))
                .Configure(app => app.UseStaticFiles(new StaticFileOptions
                {
                    RequestPath = new PathString(baseUrl),
                }));
            }).Build();

        await host.StartAsync();

        var hostingEnvironment = host.Services.GetService<IWebHostEnvironment>();

        using (var client = new HttpClient { BaseAddress = new Uri(Helpers.GetAddress(host)) })
        {
            var fileInfo = hostingEnvironment.WebRootFileProvider.GetFileInfo(Path.GetFileName(requestUrl));
            var response = await client.GetAsync(requestUrl);
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
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureServices(services => services.AddSingleton(LoggerFactory))
                .UseKestrel()
                .UseUrls(TestUrlHelper.GetTestUrl(ServerType.Kestrel))
                .UseWebRoot(Path.Combine(AppContext.BaseDirectory, baseDir))
                .Configure(app => app.UseStaticFiles(new StaticFileOptions
                {
                    RequestPath = new PathString(baseUrl),
                }));
            }).Build();

        await host.StartAsync();

        var hostingEnvironment = host.Services.GetService<IWebHostEnvironment>();

        using (var client = new HttpClient { BaseAddress = new Uri(Helpers.GetAddress(host)) })
        {
            var fileInfo = hostingEnvironment.WebRootFileProvider.GetFileInfo(Path.GetFileName(requestUrl));
            var request = new HttpRequestMessage(HttpMethod.Head, requestUrl);
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
            Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
        }
    }

    public static IEnumerable<object[]> ExistingFiles => new[]
    {
            new[] {"", @".", "/TestDocument.txt"},
            new[] {"/somedir", @".", "/somedir/TestDocument.txt"},
            new[] {"/SomeDir", @".", "/soMediR/TestDocument.txt"},
            new[] {"", @"SubFolder", "/ranges.txt"},
            new[] {"/somedir", @"SubFolder", "/somedir/ranges.txt"},
            new[] {"", @"SubFolder", "/Empty.txt"}
        };

    [Fact]
    public Task ClientDisconnect_Kestrel_NoWriteExceptionThrown()
    {
        return ClientDisconnect_NoWriteExceptionThrown(ServerType.Kestrel);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public Task ClientDisconnect_WebListener_NoWriteExceptionThrown()
    {
        return ClientDisconnect_NoWriteExceptionThrown(ServerType.HttpSys);
    }

    private async Task ClientDisconnect_NoWriteExceptionThrown(ServerType serverType)
    {
        var interval = TimeSpan.FromSeconds(15);
        var requestReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var responseComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception exception = null;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureServices(services => services.AddSingleton(LoggerFactory))
                .UseWebRoot(Path.Combine(AppContext.BaseDirectory))
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        try
                        {
                            requestReceived.SetResult();
                            await requestCancelled.Task.TimeoutAfter(interval);
                            Assert.True(context.RequestAborted.WaitHandle.WaitOne(interval), "not aborted");
                            await next(context);
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                        responseComplete.SetResult();
                    });
                    app.UseStaticFiles();
                })
                .UseUrls(TestUrlHelper.GetTestUrl(serverType));

                if (serverType == ServerType.HttpSys)
                {
                    webHostBuilder.UseHttpSys();
                }
                else if (serverType == ServerType.Kestrel)
                {
                    webHostBuilder.UseKestrel();
                }
            }).Build();

        await host.StartAsync();

        // We don't use HttpClient here because it's disconnect behavior varies across platforms.
        var socket = SendSocketRequestAsync(Helpers.GetAddress(host), "/TestDocument1MB.txt");
        await requestReceived.Task.TimeoutAfter(interval);

        socket.LingerState = new LingerOption(true, 0);
        socket.Dispose();
        requestCancelled.SetResult();

        await responseComplete.Task.TimeoutAfter(interval);
        Assert.Null(exception);
    }

    private Socket SendSocketRequestAsync(string address, string path, string method = "GET")
    {
        var uri = new Uri(address);
        var builder = new StringBuilder();
        builder.Append(FormattableString.Invariant($"{method} {path} HTTP/1.1\r\n"));
        builder.Append(FormattableString.Invariant($"HOST: {uri.Authority}\r\n\r\n"));

        byte[] request = Encoding.ASCII.GetBytes(builder.ToString());

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(IPAddress.Loopback, uri.Port);
        socket.Send(request);
        return socket;
    }
}
