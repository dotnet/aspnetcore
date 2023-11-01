// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http2Cat;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

#if !IIS_FUNCTIONALS
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;

#if IISEXPRESS_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.IISExpress.FunctionalTests;
#elif NEWHANDLER_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewHandler.FunctionalTests;
#elif NEWSHIM_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewShim.FunctionalTests;
#endif

#else
namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;
#endif

/// <summary>
/// These are HTTP/2 tests that work on both IIS and Express. See Http2TrailerResetTests for IIS specific tests
/// with newer functionality.
/// </summary>
[Collection(IISHttpsTestSiteCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class Http2Tests
{
    public Http2Tests(IISTestSiteFixture fixture)
    {
        var port = TestPortHelper.GetNextSSLPort();
        fixture.DeploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
        fixture.DeploymentParameters.AddHttpsToServerConfig();
        fixture.DeploymentParameters.SetWindowsAuth(false);
        Fixture = fixture;
    }

    public IISTestSiteFixture Fixture { get; }

    [ConditionalTheory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("CUSTOM")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
    public async Task Http2_MethodsRequestWithoutData_Success(string method)
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                var headers = new[]
                {
                    new KeyValuePair<string, string>(InternalHeaderNames.Method, method),
                    new KeyValuePair<string, string>(InternalHeaderNames.Path, "/Http2_MethodsRequestWithoutData_Success"),
                    new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "https"),
                    new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:443"),
                };

                await h2Connection.StartStreamAsync(1, headers, endStream: true);

                await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                {
                    Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
                });

                var dataFrame = await h2Connection.ReceiveFrameAsync();

                Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalTheory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
    public async Task Http2_PostRequestWithoutData_LengthRequired(string method)
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                var headers = new[]
                {
                    new KeyValuePair<string, string>(InternalHeaderNames.Method, method),
                    new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
                    new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "https"),
                    new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:443"),
                };

                await h2Connection.StartStreamAsync(1, headers, endStream: true);

                await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                {
                    Assert.Equal("411", decodedHeaders[InternalHeaderNames.Status]);
                });

                var dataFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: false, length: 344);
                dataFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalTheory]
    [InlineData("GET")]
    // [InlineData("HEAD")] Reset with code HTTP_1_1_REQUIRED
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("CUSTOM")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "Http2 requires Win10, and older versions of Win10 send some odd empty data frames.")]
    public async Task Http2_RequestWithDataAndContentLength_Success(string method)
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                var headers = new[]
                {
                    new KeyValuePair<string, string>(InternalHeaderNames.Method, method),
                    new KeyValuePair<string, string>(InternalHeaderNames.Path, "/Http2_RequestWithDataAndContentLength_Success"),
                    new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "https"),
                    new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:443"),
                    new KeyValuePair<string, string>(HeaderNames.ContentLength, "11"),
                };

                await h2Connection.StartStreamAsync(1, headers, endStream: false);

                await h2Connection.SendDataAsync(1, Encoding.UTF8.GetBytes("Hello World"), endStream: true);

                // Http.Sys no longer sends a window update here on later versions.
                if (Environment.OSVersion.Version < new Version(10, 0, 19041, 0))
                {
                    var windowUpdate = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.WINDOW_UPDATE, windowUpdate.Type);
                }

                await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                {
                    Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
                });

                var dataFrame = await h2Connection.ReceiveFrameAsync();
                Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                Assert.Equal(1, dataFrame.StreamId);

                // Some versions send an empty data frame first.
                if (dataFrame.PayloadLength == 0)
                {
                    Assert.False(dataFrame.DataEndStream);
                    dataFrame = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                    Assert.Equal(1, dataFrame.StreamId);
                }

                Assert.Equal(11, dataFrame.PayloadLength);
                Assert.Equal("Hello World", Encoding.UTF8.GetString(dataFrame.Payload.Span));

                if (!dataFrame.DataEndStream)
                {
                    dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);
                }

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalTheory]
    [InlineData("GET")]
    // [InlineData("HEAD")] Reset with code HTTP_1_1_REQUIRED
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("CUSTOM")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "Http2 requires Win10, and older versions of Win10 send some odd empty data frames.")]
    public async Task Http2_RequestWithDataAndNoContentLength_Success(string method)
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                var headers = new[]
                {
                    new KeyValuePair<string, string>(InternalHeaderNames.Method, method),
                    new KeyValuePair<string, string>(InternalHeaderNames.Path, "/Http2_RequestWithDataAndNoContentLength_Success"),
                    new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "https"),
                    new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:443"),
                };

                await h2Connection.StartStreamAsync(1, headers, endStream: false);

                await h2Connection.SendDataAsync(1, Encoding.UTF8.GetBytes("Hello World"), endStream: true);

                // Http.Sys no longer sends a window update here on later versions.
                if (Environment.OSVersion.Version < new Version(10, 0, 19041, 0))
                {
                    var windowUpdate = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.WINDOW_UPDATE, windowUpdate.Type);
                }

                await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                {
                    Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
                });

                var dataFrame = await h2Connection.ReceiveFrameAsync();
                Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                Assert.Equal(1, dataFrame.StreamId);

                // Some versions send an empty data frame first.
                if (dataFrame.PayloadLength == 0)
                {
                    Assert.False(dataFrame.DataEndStream);
                    dataFrame = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                    Assert.Equal(1, dataFrame.StreamId);
                }

                Assert.Equal(11, dataFrame.PayloadLength);
                Assert.Equal("Hello World", Encoding.UTF8.GetString(dataFrame.Payload.Span));

                if (!dataFrame.DataEndStream)
                {
                    dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);
                }

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "Http2 requires Win10, and older versions of Win10 send some odd empty data frames.")]
    public async Task Http2_ResponseWithData_Success()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetHeaders("/Http2_ResponseWithData_Success"), endStream: true);

                await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                {
                    Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
                });

                var dataFrame = await h2Connection.ReceiveFrameAsync();
                Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                Assert.Equal(1, dataFrame.StreamId);

                // Some versions send an empty data frame first.
                if (dataFrame.PayloadLength == 0)
                {
                    Assert.False(dataFrame.DataEndStream);
                    dataFrame = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                    Assert.Equal(1, dataFrame.StreamId);
                }

                Assert.Equal(11, dataFrame.PayloadLength);
                Assert.Equal("Hello World", Encoding.UTF8.GetString(dataFrame.Payload.Span));

                if (!dataFrame.DataEndStream)
                {
                    dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);
                }

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    public async Task ResponseTrailers_HTTP1_TrailersNotAvailable()
    {
        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_HTTP1_TrailersNotAvailable", http2: false);

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version11, response.Version);
        Assert.Empty(response.TrailingHeaders);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
    public async Task AppException_BeforeResponseHeaders_500()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetHeaders("/AppException_BeforeResponseHeaders_500"), endStream: true);

                await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                {
                    Assert.Equal("500", decodedHeaders[InternalHeaderNames.Status]);
                });

                var dataFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task Reset_Http1_NotSupported()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(200) };
        client.DefaultRequestVersion = HttpVersion.Version11;
        var response = await client.GetStringAsync(Fixture.Client.BaseAddress + "Reset_Http1_NotSupported");
        Assert.Equal("Hello World", response);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "This is last version without Reset support")]
    public async Task Reset_PriorOSVersions_NotSupported()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(200) };
        client.DefaultRequestVersion = HttpVersion.Version20;
        var response = await client.GetStringAsync(Fixture.Client.BaseAddress + "Reset_PriorOSVersions_NotSupported");
        Assert.Equal("Hello World", response);
    }

    private static List<KeyValuePair<string, string>> GetHeaders(string path)
    {
        var headers = Headers.ToList();

        var kvp = new KeyValuePair<string, string>(InternalHeaderNames.Path, path);
        headers.Add(kvp);
        return headers;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool http2 = true)
    {
        var handler = new HttpClientHandler();
        handler.MaxResponseHeadersLength = 128;
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(200) };
        client.DefaultRequestVersion = http2 ? HttpVersion.Version20 : HttpVersion.Version11;
        return await client.GetAsync(uri);
    }

    private static readonly IEnumerable<KeyValuePair<string, string>> Headers = new[]
    {
        new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
        new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "https"),
        new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:443"),
        new KeyValuePair<string, string>("user-agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:54.0) Gecko/20100101 Firefox/54.0"),
        new KeyValuePair<string, string>("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"),
        new KeyValuePair<string, string>("accept-language", "en-US,en;q=0.5"),
        new KeyValuePair<string, string>("accept-encoding", "gzip, deflate, br"),
        new KeyValuePair<string, string>("upgrade-insecure-requests", "1"),
    };
}
