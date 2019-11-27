// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http2Cat;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests
{
    public class Http2Tests
    {
        [ConditionalFact(Skip = "https://github.com/aspnet/AspNetCore/issues/17420")]
        // TODO: Max OS Version attribute 19H1
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
        public async Task ConnectionClose_NoOSSupport_NoGoAway()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                httpContext.Response.Headers[HeaderNames.Connection] = "close";
                return Task.FromResult(0);
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    var headersFrame = await h2Connection.ReceiveFrameAsync();

                    Assert.Equal(Http2FrameType.HEADERS, headersFrame.Type);
                    Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_HEADERS) != 0);
                    Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_STREAM) != 0);

                    h2Connection.Logger.LogInformation("Received headers in a single frame.");

                    var decodedHeaders = h2Connection.DecodeHeaders(headersFrame);

                    // HTTP/2 filters out the connection header
                    Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                    Assert.Equal("200", decodedHeaders[HeaderNames.Status]);

                    // Send and receive a second request to ensure there is no GoAway frame on the wire yet.

                    await h2Connection.StartStreamAsync(3, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    headersFrame = await h2Connection.ReceiveFrameAsync();

                    Assert.Equal(Http2FrameType.HEADERS, headersFrame.Type);
                    Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_HEADERS) != 0);
                    Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_STREAM) != 0);

                    h2Connection.Logger.LogInformation("Received headers in a single frame.");

                    h2Connection.ResetHeaders();
                    decodedHeaders = h2Connection.DecodeHeaders(headersFrame);

                    // HTTP/2 filters out the connection header
                    Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                    Assert.Equal("200", decodedHeaders[HeaderNames.Status]);

                    await h2Connection.StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_19H2, SkipReason = "GoAway support was added in Win10_19H2.")]
        public async Task ConnectionClose_OSSupport_SendsGoAway()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                httpContext.Response.Headers[HeaderNames.Connection] = "close";
                return Task.FromResult(0);
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    var goAwayFrame = await h2Connection.ReceiveFrameAsync();
                    h2Connection.VerifyGoAway(goAwayFrame, int.MaxValue, Http2ErrorCode.NO_ERROR);

                    var headersFrame = await h2Connection.ReceiveFrameAsync();

                    Assert.Equal(Http2FrameType.HEADERS, headersFrame.Type);
                    Assert.Equal(Http2HeadersFrameFlags.END_HEADERS, headersFrame.HeadersFlags);

                    h2Connection.Logger.LogInformation("Received headers in a single frame.");

                    var decodedHeaders = h2Connection.DecodeHeaders(headersFrame);

                    // HTTP/2 filters out the connection header
                    Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                    Assert.Equal("200", decodedHeaders[HeaderNames.Status]);

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                    Assert.Equal(Http2DataFrameFlags.END_STREAM, dataFrame.DataFlags);
                    Assert.Equal(0, dataFrame.PayloadLength);

                    // Http.Sys doesn't send a final GoAway unless we ignore the first one and send 200 additional streams.

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_19H2, SkipReason = "GoAway support was added in Win10_19H2.")]
        public async Task ConnectionClose_AdditionalRequests_ReceivesSecondGoAway()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                httpContext.Response.Headers[HeaderNames.Connection] = "close";
                return Task.FromResult(0);
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    var streamId = 1;
                    await h2Connection.StartStreamAsync(streamId, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    var goAwayFrame = await h2Connection.ReceiveFrameAsync();
                    h2Connection.VerifyGoAway(goAwayFrame, int.MaxValue, Http2ErrorCode.NO_ERROR);

                    var headersFrame = await h2Connection.ReceiveFrameAsync();

                    Assert.Equal(Http2FrameType.HEADERS, headersFrame.Type);
                    Assert.Equal(Http2HeadersFrameFlags.END_HEADERS, headersFrame.HeadersFlags);
                    Assert.Equal(streamId, headersFrame.StreamId);

                    h2Connection.Logger.LogInformation("Received headers in a single frame.");

                    var decodedHeaders = h2Connection.DecodeHeaders(headersFrame);

                    // HTTP/2 filters out the connection header
                    Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                    Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    h2Connection.ResetHeaders();

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                    Assert.Equal(Http2DataFrameFlags.END_STREAM, dataFrame.DataFlags);
                    Assert.Equal(0, dataFrame.PayloadLength);
                    Assert.Equal(streamId, dataFrame.StreamId);

                    // Http.Sys doesn't send a final GoAway unless we ignore the first one and send 200 additional streams.

                    for (var i = 1; i < 200; i++)
                    {
                        streamId = 1 + (i * 2); // Odds.
                        await h2Connection.StartStreamAsync(streamId, Http2Utilities.BrowserRequestHeaders, endStream: true);

                        headersFrame = await h2Connection.ReceiveFrameAsync();

                        Assert.Equal(Http2FrameType.HEADERS, headersFrame.Type);
                        Assert.Equal(Http2HeadersFrameFlags.END_HEADERS, headersFrame.HeadersFlags);
                        Assert.Equal(streamId, headersFrame.StreamId);

                        h2Connection.Logger.LogInformation("Received headers in a single frame.");

                        decodedHeaders = h2Connection.DecodeHeaders(headersFrame);

                        // HTTP/2 filters out the connection header
                        Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                        h2Connection.ResetHeaders();

                        dataFrame = await h2Connection.ReceiveFrameAsync();
                        Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                        Assert.Equal(Http2DataFrameFlags.END_STREAM, dataFrame.DataFlags);
                        Assert.Equal(0, dataFrame.PayloadLength);
                        Assert.Equal(streamId, dataFrame.StreamId);
                    }

                    streamId = 1 + (200 * 2); // Odds.
                    await h2Connection.StartStreamAsync(streamId, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    // Final GoAway
                    goAwayFrame = await h2Connection.ReceiveFrameAsync();
                    h2Connection.VerifyGoAway(goAwayFrame, streamId, Http2ErrorCode.NO_ERROR);

                    // Normal response
                    headersFrame = await h2Connection.ReceiveFrameAsync();

                    Assert.Equal(Http2FrameType.HEADERS, headersFrame.Type);
                    Assert.Equal(Http2HeadersFrameFlags.END_HEADERS, headersFrame.HeadersFlags);
                    Assert.Equal(streamId, headersFrame.StreamId);

                    h2Connection.Logger.LogInformation("Received headers in a single frame.");

                    decodedHeaders = h2Connection.DecodeHeaders(headersFrame);

                    // HTTP/2 filters out the connection header
                    Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                    Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    h2Connection.ResetHeaders();

                    dataFrame = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                    Assert.Equal(Http2DataFrameFlags.END_STREAM, dataFrame.DataFlags);
                    Assert.Equal(0, dataFrame.PayloadLength);
                    Assert.Equal(streamId, dataFrame.StreamId);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }
    }
}
