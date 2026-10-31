// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebSockets.Test;

public class WebSocketCompressionMiddlewareTests : LoggedTest
{
    [Fact]
    public async Task CompressionNegotiationServerCanChooseSevrverNoContextTakeover()
    {
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext()
            {
                DangerousEnableCompression = true,
                DisableServerContextTakeover = true
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
                    request.Headers.Add(HeaderNames.SecWebSocketExtensions, "permessage-deflate");

                    var response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.SwitchingProtocols, response.StatusCode);
                    Assert.Equal("permessage-deflate; server_no_context_takeover", response.Headers.GetValues(HeaderNames.SecWebSocketExtensions).Aggregate((l, r) => $"{l}; {r}"));
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
    [InlineData("permessage-deflate; server_max_window_bits=14, permessage-deflate; server_max_window_bits=13", "permessage-deflate; server_max_window_bits=13")]
    [InlineData("permessage-deflate; client_max_window_bits=8, permessage-deflate; client_max_window_bits=13", "permessage-deflate; client_max_window_bits=13; server_max_window_bits=13")]
    public async Task CompressionNegotiationCanChooseExtension(string clientHeader, string expectedResponse)
    {
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext()
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
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext()
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
