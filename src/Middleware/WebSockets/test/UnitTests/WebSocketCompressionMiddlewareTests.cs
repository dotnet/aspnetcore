// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.WebSockets.Test
{
    public class WebSocketCompressionMiddlewareTests : LoggedTest
    {
        [Theory]
        [InlineData("permessage-deflate", "permessage-deflate")]
        //[InlineData("permessage-deflate; server_no_context_takeover", "permessage-deflate; server_no_context_takeover")]
        [InlineData("permessage-deflate; client_no_context_takeover", "permessage-deflate; client_no_context_takeover")]
        [InlineData("permessage-deflate; client_max_window_bits=9", "permessage-deflate; client_max_window_bits=9")]
        [InlineData("permessage-deflate; client_max_window_bits", "permessage-deflate; client_max_window_bits=15")]
        [InlineData("permessage-deflate; server_max_window_bits", "permessage-deflate; server_max_window_bits=15")]
        [InlineData("permessage-deflate; server_max_window_bits=10", "permessage-deflate; server_max_window_bits=10")]
        //[InlineData("permessage-deflate; server_max_window_bits=10; server_no_context_takeover", "permessage-deflate; server_no_context_takeover; server_max_window_bits=10")]
        //[InlineData("permessage-deflate; server_max_window_bits=10; server_no_context_takeover; client_no_context_takeover; client_max_window_bits=12", "permessage-deflate; server_no_context_takeover; client_no_context_takeover; client_max_window_bits=12; server_max_window_bits=10")]
        public async Task CompressionNegotiationProducesCorrectHeaderWithDefaultOptions(string clientHeader, string expectedResponse)
        {
            await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync(new ExtendedWebSocketAcceptContext()
                {
                    DangerousEnableCompression = true
                });
            }))
            {
                using (var client = new HttpClient())
                {
                    var uri = new UriBuilder(new Uri($"ws://127.0.0.1:{port}/"));
                    uri.Scheme = "http";

                    // Craft a valid WebSocket Upgrade request
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString()))
                    {
                        SetGenericWebSocketRequest(request);
                        request.Headers.Add(HeaderNames.SecWebSocketExtensions, clientHeader);

                        var response = await client.SendAsync(request);
                        Assert.Equal(HttpStatusCode.SwitchingProtocols, response.StatusCode);
                        Assert.Equal(expectedResponse, response.Headers.GetValues(HeaderNames.SecWebSocketExtensions).Aggregate((l, r) => $"{l}; {r}"));
                    }
                }
            }
        }

        [Theory]
        [InlineData("permessage-deflate", "permessage-deflate; server_max_window_bits=14")]
        //[InlineData("permessage-deflate; server_no_context_takeover", "permessage-deflate; server_no_context_takeover; server_max_window_bits=14")]
        [InlineData("permessage-deflate; client_no_context_takeover", "permessage-deflate; client_no_context_takeover; server_max_window_bits=14")]
        [InlineData("permessage-deflate; client_max_window_bits=9", "permessage-deflate; client_max_window_bits=9; server_max_window_bits=14")]
        [InlineData("permessage-deflate; client_max_window_bits", "permessage-deflate; client_max_window_bits=15; server_max_window_bits=14")]
        [InlineData("permessage-deflate; server_max_window_bits", "permessage-deflate; server_max_window_bits=14")]
        [InlineData("permessage-deflate; server_max_window_bits=14", "permessage-deflate; server_max_window_bits=14")]
        [InlineData("permessage-deflate; server_max_window_bits=10", "permessage-deflate; server_max_window_bits=10")]
        //[InlineData("permessage-deflate; server_max_window_bits=10; server_no_context_takeover", "permessage-deflate; server_no_context_takeover; server_max_window_bits=10")]
        [InlineData("permessage-deflate; server_max_window_bits=10; client_no_context_takeover; client_max_window_bits=12", "permessage-deflate; client_no_context_takeover; client_max_window_bits=12; server_max_window_bits=10")]
        public async Task CompressionNegotiationProducesCorrectHeaderWithCustomOptions(string clientHeader, string expectedResponse)
        {
            await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync(new ExtendedWebSocketAcceptContext()
                {
                        DangerousEnableCompression = true,
                        ServerMaxWindowBits = 14
                });
            }))
            {
                using (var client = new HttpClient())
                {
                    var uri = new UriBuilder(new Uri($"ws://127.0.0.1:{port}/"));
                    uri.Scheme = "http";

                    // Craft a valid WebSocket Upgrade request
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString()))
                    {
                        SetGenericWebSocketRequest(request);
                        request.Headers.Add(HeaderNames.SecWebSocketExtensions, clientHeader);

                        var response = await client.SendAsync(request);
                        Assert.Equal(HttpStatusCode.SwitchingProtocols, response.StatusCode);
                        Assert.Equal(expectedResponse, response.Headers.GetValues(HeaderNames.SecWebSocketExtensions).Aggregate((l, r) => $"{l}; {r}"));
                    }
                }
            }
        }

        [Fact]
        public async Task CompressionNegotiationIgnoredIfNotEnabledOnServer()
        {
            await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            }))
            {
                using (var client = new HttpClient())
                {
                    var uri = new UriBuilder(new Uri($"ws://127.0.0.1:{port}/"));
                    uri.Scheme = "http";

                    // Craft a valid WebSocket Upgrade request
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString()))
                    {
                        SetGenericWebSocketRequest(request);
                        request.Headers.Add(HeaderNames.SecWebSocketExtensions, "permessage-deflate");

                        var response = await client.SendAsync(request);
                        Assert.Equal(HttpStatusCode.SwitchingProtocols, response.StatusCode);
                        Assert.False(response.Headers.Contains(HeaderNames.SecWebSocketExtensions));
                    }
                }
            }
        }

        [Theory]
        [InlineData("permessage-deflate; server_max_window_bits=8")]
        [InlineData("permessage-deflate; client_max_window_bits=8")]
        public async Task CompressionNegotiateNotAccepted(string clientHeader)
        {
            await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync(new ExtendedWebSocketAcceptContext()
                {
                    DangerousEnableCompression = true,
                    ServerMaxWindowBits = 11
                });
            }))
            {
                using (var client = new HttpClient())
                {
                    var uri = new UriBuilder(new Uri($"ws://127.0.0.1:{port}/"));
                    uri.Scheme = "http";

                    // Craft a valid WebSocket Upgrade request
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString()))
                    {
                        SetGenericWebSocketRequest(request);
                        request.Headers.Add(HeaderNames.SecWebSocketExtensions, clientHeader);

                        var response = await client.SendAsync(request);
                        Assert.Equal(HttpStatusCode.SwitchingProtocols, response.StatusCode);
                        Assert.False(response.Headers.Contains(HeaderNames.SecWebSocketExtensions));
                    }
                }
            }
        }

        [Theory]
        [InlineData("permessage-deflate; server_max_window_bits=16", "invalid server_max_window_bits used: 16")]
        [InlineData("permessage-deflate; server_max_window_bits=7", "invalid server_max_window_bits used: 7")]
        [InlineData("permessage-deflate; client_max_window_bits=16", "invalid client_max_window_bits used: 16")]
        [InlineData("permessage-deflate; client_max_window_bits=7", "invalid client_max_window_bits used: 7")]
        public async Task InvalidCompressionNegotiateThrows(string clientHeader, string errorMessage)
        {
            await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var ex = await Assert.ThrowsAsync<WebSocketException>(() => context.WebSockets.AcceptWebSocketAsync(new ExtendedWebSocketAcceptContext()
                {
                    DangerousEnableCompression = true
                }));
                Assert.Equal(errorMessage, ex.Message);
            }))
            {
                using (var client = new HttpClient())
                {
                    var uri = new UriBuilder(new Uri($"ws://127.0.0.1:{port}/"));
                    uri.Scheme = "http";

                    // Craft a valid WebSocket Upgrade request
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString()))
                    {
                        SetGenericWebSocketRequest(request);
                        request.Headers.Add(HeaderNames.SecWebSocketExtensions, clientHeader);

                        var response = await client.SendAsync(request);
                    }
                }
            }
        }

        [Theory]
        [InlineData("permessage-deflate; server_max_window_bits=8, permessage-deflate; server_max_window_bits=13", "permessage-deflate; server_max_window_bits=13")]
        [InlineData("permessage-deflate; client_max_window_bits=8, permessage-deflate; client_max_window_bits=13", "permessage-deflate; client_max_window_bits=13; server_max_window_bits=13")]
        public async Task CompressionNegotiationCanChooseExtension(string clientHeader, string expectedResponse)
        {
            await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync(new ExtendedWebSocketAcceptContext()
                {
                    DangerousEnableCompression = true,
                    ServerMaxWindowBits = 13
                });
            }))
            {
                using (var client = new HttpClient())
                {
                    var uri = new UriBuilder(new Uri($"ws://127.0.0.1:{port}/"));
                    uri.Scheme = "http";

                    // Craft a valid WebSocket Upgrade request
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString()))
                    {
                        SetGenericWebSocketRequest(request);
                        request.Headers.Add(HeaderNames.SecWebSocketExtensions, clientHeader);

                        var response = await client.SendAsync(request);
                        Assert.Equal(HttpStatusCode.SwitchingProtocols, response.StatusCode);
                        Assert.Equal(expectedResponse, response.Headers.GetValues(HeaderNames.SecWebSocketExtensions).Aggregate((l, r) => $"{l}; {r}"));
                    }
                }
            }
        }

        // Smoke test that compression works, we aren't responsible for the specifics of the compression frames
        [Fact]
        public async Task CanSendAndReceiveCompressedData()
        {
            await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync(new ExtendedWebSocketAcceptContext()
                {
                    DangerousEnableCompression = true,
                    ServerMaxWindowBits = 13
                });

                var serverBuffer = new byte[1024];
                while (true)
                {
                    var result = await webSocket.ReceiveAsync(serverBuffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    await webSocket.SendAsync(serverBuffer.AsMemory(0, result.Count), result.MessageType, result.EndOfMessage, default);
                }
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    client.Options.DangerousDeflateOptions = new WebSocketDeflateOptions()
                    {
                        ServerMaxWindowBits = 12,
                        ClientMaxWindowBits = 11,
                    };
                    await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                    var sendCount = 8193;
                    var clientBuf = new byte[sendCount];
                    var receiveBuf = new byte[sendCount];
                    Random.Shared.NextBytes(clientBuf);
                    while (sendCount > 0)
                    {
                        await client.SendAsync(clientBuf.AsMemory(0, sendCount), WebSocketMessageType.Binary, true, default);
                        var totalRecv = 0;
                        while (totalRecv < sendCount)
                        {
                            var result = await client.ReceiveAsync(receiveBuf.AsMemory(totalRecv), default);
                            totalRecv += result.Count;
                            if (result.EndOfMessage)
                            {
                                Assert.Equal(sendCount, totalRecv);
                                for (var i = 0; i < sendCount; ++i)
                                {
                                    Assert.True(clientBuf[i] == receiveBuf[i], $"offset {i} not equal: {clientBuf[i]} == {receiveBuf[i]}");
                                }
                            }
                        }

                        sendCount -= 13;
                    }

                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
                }
            }
        }

        private static void SetGenericWebSocketRequest(HttpRequestMessage request)
        {
            request.Headers.Connection.Clear();
            request.Headers.Connection.Add("Upgrade");
            request.Headers.Connection.Add("keep-alive");
            request.Headers.Upgrade.Add(new System.Net.Http.Headers.ProductHeaderValue("websocket"));
            request.Headers.Add(HeaderNames.SecWebSocketVersion, "13");
            // SecWebSocketKey required to be 16 bytes
            request.Headers.Add(HeaderNames.SecWebSocketKey, Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }, Base64FormattingOptions.None));
        }
    }
}
