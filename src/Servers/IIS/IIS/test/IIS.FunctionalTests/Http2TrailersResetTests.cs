// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http2Cat;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

/// <summary>
/// These features/tests Are only supported on newer versions of Windows and IIS. They are not supported
/// on IIS Express even on the new Windows versions because IIS Express has its own outdated copy of IIS.
/// </summary>
[Collection(IISHttpsTestSiteCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class Http2TrailerResetTests : FunctionalTestsBase
{
    private const string WindowsVersionForTrailers = "10.0.20238";

    public Http2TrailerResetTests(IISTestSiteFixture fixture)
    {
        var port = TestPortHelper.GetNextSSLPort();
        fixture.DeploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
        fixture.DeploymentParameters.AddHttpsToServerConfig();
        fixture.DeploymentParameters.SetWindowsAuth(false);
        Fixture = fixture;
    }

    public IISTestSiteFixture Fixture { get; }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task ResponseTrailers_HTTP2_TrailersAvailable()
    {
        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_HTTP2_TrailersAvailable");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        Assert.Empty(response.TrailingHeaders);
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task ResponseTrailers_ProhibitedTrailers_Blocked()
    {
        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_ProhibitedTrailers_Blocked");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        Assert.Empty(response.TrailingHeaders);
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task ResponseTrailers_NoBody_TrailersSent()
    {
        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_NoBody_TrailersSent");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        Assert.NotEmpty(response.TrailingHeaders);
        Assert.Equal("TrailerValue", response.TrailingHeaders.GetValues("TrailerName").Single());
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task ResponseTrailers_WithBody_TrailersSent()
    {
        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_WithBody_TrailersSent");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(response.TrailingHeaders);
        Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task ResponseTrailers_WithContentLengthBody_TrailersSent()
    {
        var body = "Hello World";

        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_WithContentLengthBody_TrailersSent");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        Assert.Equal(body, await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(response.TrailingHeaders);
        Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task ResponseTrailers_WithTrailersBeforeContentLengthBody_TrailersSent()
    {
        var body = "Hello World";

        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_WithTrailersBeforeContentLengthBody_TrailersSent");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        // Avoid HttpContent's automatic content-length calculation.
        Assert.True(response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out var contentLength), HeaderNames.ContentLength);
        Assert.Equal((2 * body.Length).ToString(CultureInfo.InvariantCulture), contentLength.First());
        Assert.Equal(body + body, await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(response.TrailingHeaders);
        Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task ResponseTrailers_WithContentLengthBodyAndDeclared_TrailersSent()
    {
        var body = "Hello World";

        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_WithContentLengthBodyAndDeclared_TrailersSent");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        // Avoid HttpContent's automatic content-length calculation.
        Assert.True(response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out var contentLength), HeaderNames.ContentLength);
        Assert.Equal(body.Length.ToString(CultureInfo.InvariantCulture), contentLength.First());
        Assert.Equal("TrailerName", response.Headers.Trailer.Single());
        Assert.Equal(body, await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(response.TrailingHeaders);
        Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task ResponseTrailers_MultipleValues_SentAsSeparateHeaders()
    {
        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_MultipleValues_SentAsSeparateHeaders");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        Assert.NotEmpty(response.TrailingHeaders);

        Assert.Equal(new[] { "TrailerValue0", "TrailerValue1" }, response.TrailingHeaders.GetValues("TrailerName"));
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task ResponseTrailers_CompleteAsyncNoBody_TrailersSent()
    {
        // The app func for CompleteAsync will not finish until CompleteAsync_Completed is sent.
        // This verifies that the response is sent to the client with CompleteAsync
        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_CompleteAsyncNoBody_TrailersSent");
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        Assert.NotEmpty(response.TrailingHeaders);
        Assert.Equal("TrailerValue", response.TrailingHeaders.GetValues("TrailerName").Single());

        var response2 = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_CompleteAsyncNoBody_TrailersSent_Completed");
        Assert.True(response2.IsSuccessStatusCode);
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task ResponseTrailers_CompleteAsyncWithBody_TrailersSent()
    {
        // The app func for CompleteAsync will not finish until CompleteAsync_Completed is sent.
        // This verifies that the response is sent to the client with CompleteAsync
        var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_CompleteAsyncWithBody_TrailersSent");
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version20, response.Version);
        Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(response.TrailingHeaders);
        Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());

        var response2 = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "ResponseTrailers_CompleteAsyncWithBody_TrailersSent_Completed");
        Assert.True(response2.IsSuccessStatusCode);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task AppException_AfterHeaders_ResetInternalError()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetHeaders("/AppException_AfterHeaders_ResetInternalError"), endStream: true);

                await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                {
                    Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
                });

                var frame = await h2Connection.ReceiveFrameAsync();

                Http2Utilities.VerifyResetFrame(frame, expectedStreamId: 1, Http2ErrorCode.INTERNAL_ERROR);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task Reset_BeforeResponse_Resets()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetHeaders("/Reset_BeforeResponse_Resets"), endStream: true);

                var resetFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);

                // Any app errors?
                var client = CreateClient();
                var response = await client.GetAsync(Fixture.Client.BaseAddress + "/Reset_BeforeResponse_Resets_Complete");
                Assert.True(response.IsSuccessStatusCode);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task RequestClose_SendsGoAway()
    {
        await new HostBuilder()
          .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
          {
              await h2Connection.InitializeConnectionAsync();

              h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

              await h2Connection.StartStreamAsync(1, GetHeaders("/ConnectionRequestClose"), endStream: true);

              var goAwayFrame = await h2Connection.ReceiveFrameAsync();
              Http2Utilities.VerifyGoAway(goAwayFrame, int.MaxValue, Http2ErrorCode.NO_ERROR);

              await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
              {
                  // HTTP/2 filters out the connection header
                  Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                  Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
              });

              var dataFrame = await h2Connection.ReceiveFrameAsync();
              Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

              // Http.Sys doesn't send a final GoAway unless we ignore the first one and send 200 additional streams.

              h2Connection.Logger.LogInformation("Connection stopped.");
          })
          .Build().RunAsync();
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task Reset_BeforeResponse_Zero_Resets()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetHeaders("/Reset_BeforeResponse_Zero_Resets"), endStream: true);

                var resetFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)0);

                // Any app errors?
                var client = CreateClient();
                var response = await client.GetAsync(Fixture.Client.BaseAddress + "/Reset_BeforeResponse_Zero_Resets_Complete");
                Assert.True(response.IsSuccessStatusCode);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task Reset_AfterResponseHeaders_Resets()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetHeaders("/Reset_AfterResponseHeaders_Resets"), endStream: true);

                // Any app errors?
                var client = CreateClient();
                var response = await client.GetAsync(Fixture.Client.BaseAddress + "/Reset_AfterResponseHeaders_Resets_Complete");
                Assert.True(response.IsSuccessStatusCode);

                await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                {
                    Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
                });

                var resetFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task Reset_DuringResponseBody_Resets()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetHeaders("/Reset_DuringResponseBody_Resets"), endStream: true);

                // This is currently flaky, can either receive header or reset at this point
                var headerOrResetFrame = await h2Connection.ReceiveFrameAsync();
                Assert.True(headerOrResetFrame.Type == Http2FrameType.HEADERS || headerOrResetFrame.Type == Http2FrameType.RST_STREAM);

                if (headerOrResetFrame.Type == Http2FrameType.HEADERS)
                {
                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: false, length: 11);

                    var resetFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);
                }
                else
                {
                    Http2Utilities.VerifyResetFrame(headerOrResetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);
                }

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task Reset_BeforeRequestBody_Resets()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetPostHeaders("/Reset_BeforeRequestBody_Resets"), endStream: false);

                // Any app errors?
                //Assert.Equal(0, await appResult.Task.DefaultTimeout());

                var resetFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task Reset_DuringRequestBody_Resets()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetPostHeaders("/Reset_DuringRequestBody_Resets"), endStream: false);
                await h2Connection.SendDataAsync(1, new byte[10], endStream: false);

                // Any app errors?
                //Assert.Equal(0, await appResult.Task.DefaultTimeout());

                var resetFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task Reset_AfterCompleteAsync_NoReset()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetHeaders("/Reset_AfterCompleteAsync_NoReset"), endStream: true);

                await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                {
                    Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
                });

                var dataFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: false, length: 11);

                dataFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
    public async Task Reset_CompleteAsyncDuringRequestBody_Resets()
    {
        await new HostBuilder()
            .UseHttp2Cat(Fixture.Client.BaseAddress.AbsoluteUri, async h2Connection =>
            {
                await h2Connection.InitializeConnectionAsync();

                h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                await h2Connection.StartStreamAsync(1, GetPostHeaders("/Reset_CompleteAsyncDuringRequestBody_Resets"), endStream: false);
                await h2Connection.SendDataAsync(1, new byte[10], endStream: false);

                await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                {
                    Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
                });

                var dataFrame = await h2Connection.ReceiveFrameAsync();

                Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                var resetFrame = await h2Connection.ReceiveFrameAsync();
                Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: Http2ErrorCode.NO_ERROR);

                h2Connection.Logger.LogInformation("Connection stopped.");
            })
            .Build().RunAsync();
    }

    private static List<KeyValuePair<string, string>> GetHeaders(string path)
    {
        var headers = Headers.ToList();

        var kvp = new KeyValuePair<string, string>(InternalHeaderNames.Path, path);
        headers.Add(kvp);
        return headers;
    }

    private static List<KeyValuePair<string, string>> GetPostHeaders(string path)
    {
        var headers = PostRequestHeaders.ToList();

        var kvp = new KeyValuePair<string, string>(InternalHeaderNames.Path, path);
        headers.Add(kvp);
        return headers;
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler();
        handler.MaxResponseHeadersLength = 128;
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(200) };
        return client;
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

    private static readonly IEnumerable<KeyValuePair<string, string>> PostRequestHeaders = new[]
    {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "https"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };
}
