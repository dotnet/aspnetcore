// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using http2cat;
using Microsoft.AspNetCore.Hosting;
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
        [ConditionalFact]
        // TODO: Max OS Version attribute
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
        public async Task ConnectionClose_NoOSSupport_NoGoAway()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                httpContext.Response.Headers[HeaderNames.Connection] = "close";
                return Task.FromResult(0);
            });

            using var host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.UseHttp2Cat(options =>
                    {
                        options.Url = address;
                        options.Scenaro = async (http2Utilities, logger) =>
                        {
                            await http2Utilities.InitializeConnectionAsync();

                            logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                            await http2Utilities.StartStreamAsync(1, Http2Utilities._browserRequestHeaders, endStream: true);

                            var headersFrame = await http2Utilities.ReceiveFrameAsync();

                            Assert.Equal(Http2FrameType.HEADERS, headersFrame.Type);
                            Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_HEADERS) != 0);
                            Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_STREAM) != 0);

                            logger.LogInformation("Received headers in a single frame.");

                            var decodedHeaders = http2Utilities.DecodeHeaders(headersFrame);

                            // HTTP/2 filters out the connection header
                            Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                            Assert.Equal("200", decodedHeaders[HeaderNames.Status]);

                            // Send and receive a second request to ensure there is no GoAway frame on the wire yet.

                            await http2Utilities.StartStreamAsync(3, Http2Utilities._browserRequestHeaders, endStream: true);

                            headersFrame = await http2Utilities.ReceiveFrameAsync();

                            Assert.Equal(Http2FrameType.HEADERS, headersFrame.Type);
                            Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_HEADERS) != 0);
                            Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_STREAM) != 0);

                            logger.LogInformation("Received headers in a single frame.");

                            http2Utilities.ResetHeaders();
                            decodedHeaders = http2Utilities.DecodeHeaders(headersFrame);

                            // HTTP/2 filters out the connection header
                            Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                            Assert.Equal("200", decodedHeaders[HeaderNames.Status]);

                            await http2Utilities.StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

                            logger.LogInformation("Connection stopped.");
                        };
                    });
                })
                .Build();

            await host.RunHttp2CatAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_19H2, SkipReason = "Http2 requires Win10")]
        public async Task ConnectionClose_OSSupport_SendsGoAway()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                httpContext.Response.Headers[HeaderNames.Connection] = "close";
                return Task.FromResult(0);
            });

            using var host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.UseHttp2Cat(options =>
                    {
                        options.Url = address;
                        options.Scenaro = async (http2Utilities, logger) =>
                        {
                            await http2Utilities.InitializeConnectionAsync();

                            logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                            await http2Utilities.StartStreamAsync(1, Http2Utilities._browserRequestHeaders, endStream: true);

                            var headersFrame = await http2Utilities.ReceiveFrameAsync();

                            Assert.Equal(Http2FrameType.HEADERS, headersFrame.Type);
                            Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_HEADERS) != 0);
                            Assert.True((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_STREAM) != 0);

                            logger.LogInformation("Received headers in a single frame.");

                            var decodedHeaders = http2Utilities.DecodeHeaders(headersFrame);

                            // HTTP/2 filters out the connection header
                            Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                            Assert.Equal("200", decodedHeaders[HeaderNames.Status]);

                            var goAwayFrame = await http2Utilities.ReceiveFrameAsync();
                            http2Utilities.VerifyGoAway(goAwayFrame, 1, Http2ErrorCode.NO_ERROR);

                            await http2Utilities.SendGoAwayAsync();
                            // TODO: Close the connection?
                            // await http2Utilities.StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

                            logger.LogInformation("Connection stopped.");
                        };
                    });
                })
                .Build();

            await host.RunAsync();
        }
    }
}
