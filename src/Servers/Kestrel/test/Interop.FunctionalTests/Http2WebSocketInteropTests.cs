// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;

namespace Interop.FunctionalTests;

/// <summary>
/// This tests interop with System.Net.Http.HttpClient (SocketHttpHandler) using HTTP/2 (H2 and H2C) WebSockets
/// https://www.rfc-editor.org/rfc/rfc8441.html
/// </summary>
public class Http2WebSocketInteropTests : LoggedTest
{
    public static IEnumerable<object[]> NegotiationScenarios
    {
        get
        {
            var list = new List<object[]>()
                {
                    new object[] { "http", "1.1", HttpVersionPolicy.RequestVersionExact, HttpProtocols.Http1, "HTTP/1.1" },
                    new object[] { "http", "2.0", HttpVersionPolicy.RequestVersionExact, HttpProtocols.Http2, "HTTP/2" },
                    new object[] { "http", "1.1", HttpVersionPolicy.RequestVersionOrHigher, HttpProtocols.Http1AndHttp2, "HTTP/1.1" }, // No TLS/APLN, Can't upgrade
                    new object[] { "http", "2.0", HttpVersionPolicy.RequestVersionOrLower, HttpProtocols.Http1AndHttp2, "HTTP/1.1" }, // No TLS/APLN, Downgrade
                };

            if (Utilities.CurrentPlatformSupportsHTTP2OverTls())
            {
                list.Add(new object[] { "https", "1.1", HttpVersionPolicy.RequestVersionExact, HttpProtocols.Http1, "HTTP/1.1" });
                list.Add(new object[] { "https", "2.0", HttpVersionPolicy.RequestVersionExact, HttpProtocols.Http2, "HTTP/2" });
                list.Add(new object[] { "https", "1.1", HttpVersionPolicy.RequestVersionOrHigher, HttpProtocols.Http1AndHttp2, "HTTP/2" }); // Upgrade
                list.Add(new object[] { "https", "2.0", HttpVersionPolicy.RequestVersionOrLower, HttpProtocols.Http1AndHttp2, "HTTP/2" });
                list.Add(new object[] { "https", "2.0", HttpVersionPolicy.RequestVersionOrLower, HttpProtocols.Http1, "HTTP/1.1" }); // Downgrade
            }

            return list;
        }
    }

    [Theory]
    [MemberData(nameof(NegotiationScenarios))]
    public async Task HttpVersionNegotationWorks(string scheme, string clientVersion, HttpVersionPolicy clientPolicy, HttpProtocols serverProtocols, string expectedVersion)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                ConfigureKestrel(webHostBuilder, scheme, serverProtocols);
                webHostBuilder.ConfigureServices(AddTestLogging)
                .Configure(app =>
                {
                    app.UseWebSockets();
                    app.Run(async context =>
                    {
                        Assert.Equal(expectedVersion, context.Request.Protocol);
                        Assert.True(context.WebSockets.IsWebSocketRequest);
                        var ws = await context.WebSockets.AcceptWebSocketAsync();
                        var bytes = new byte[1024];
                        var result = await ws.ReceiveAsync(bytes, default);
                        Assert.True(result.EndOfMessage);
                        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
                        Assert.Equal("Hello", Encoding.UTF8.GetString(bytes, 0, result.Count));

                        await ws.SendAsync(Encoding.UTF8.GetBytes("Hi there"), WebSocketMessageType.Text, endOfMessage: true, default);
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed", default);
                    });
                });
            });
        using var host = await hostBuilder.StartAsync().DefaultTimeout();

        var url = host.MakeUrl(scheme == "http" ? "ws" : "wss");
        using var client = CreateClient();
        var wsClient = new ClientWebSocket();
        wsClient.Options.HttpVersion = Version.Parse(clientVersion);
        wsClient.Options.HttpVersionPolicy = clientPolicy;
        wsClient.Options.CollectHttpResponseDetails = true;
        await wsClient.ConnectAsync(new Uri(url), client, default);
        Assert.Equal(expectedVersion == "HTTP/2" ? HttpStatusCode.OK : HttpStatusCode.SwitchingProtocols, wsClient.HttpStatusCode);

        await wsClient.SendAsync(Encoding.UTF8.GetBytes("Hello"), WebSocketMessageType.Text, endOfMessage: true, default);

        var bytes = new byte[1024];
        var result = await wsClient.ReceiveAsync(bytes, default);
        Assert.True(result.EndOfMessage);
        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        Assert.Equal("Hi there", Encoding.UTF8.GetString(bytes, 0, result.Count));

        await wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", default);
    }

    [Fact]
    public async Task PingTimeoutCancelsReceiveAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                ConfigureKestrel(webHostBuilder, "https", HttpProtocols.Http2);
                webHostBuilder.ConfigureServices(AddTestLogging)
                .Configure(app =>
                {
                    app.UseWebSockets(new WebSocketOptions()
                    {
                        KeepAliveInterval = TimeSpan.FromMilliseconds(1),
                        KeepAliveTimeout = TimeSpan.FromMilliseconds(1),
                    });
                    app.Run(async context =>
                    {
                        Assert.True(context.WebSockets.IsWebSocketRequest);
                        var ws = await context.WebSockets.AcceptWebSocketAsync();
                        var bytes = new byte[1024];

                        try
                        {
                            var result = await ws.ReceiveAsync(bytes, default);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                        finally
                        {
                            tcs.TrySetResult();
                        }
                    });
                });
            });
        using var host = await hostBuilder.StartAsync().DefaultTimeout();

        var url = host.MakeUrl("wss");

        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        var pauseSendHandler = new PauseSendHandler(handler);
        using var client = new HttpClient(pauseSendHandler);

        var wsClient = new ClientWebSocket();
        wsClient.Options.HttpVersion = Version.Parse("2.0");
        wsClient.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        wsClient.Options.CollectHttpResponseDetails = true;
        await wsClient.ConnectAsync(new Uri(url), client, default);
        Assert.Equal(HttpStatusCode.OK, wsClient.HttpStatusCode);

        // Prevent Pong replies so we can test the server timing out
        // It's fine if some Pongs were already sent before this is set
        pauseSendHandler.PauseSend = true;

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => tcs.Task);
        Assert.True(ex is WebSocketException || ex is TaskCanceledException, ex.GetType().FullName);

        // Unblock Send
        pauseSendHandler.PauseSend = false;

        // Call any websocket method that tries networking so we have something to await to check that the client connection closed.
        await Assert.ThrowsAnyAsync<Exception>(() => wsClient.ReceiveAsync(new byte[1], default));

        Assert.Equal(WebSocketState.Aborted, wsClient.State);
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        var client = new HttpClient(handler);
        return client;
    }

    private static void ConfigureKestrel(IWebHostBuilder webHostBuilder, string scheme, HttpProtocols protocols)
    {
        webHostBuilder.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = protocols;
                if (scheme == "https")
                {
                    listenOptions.UseHttps(TestResources.GetTestCertificate());
                }
            });
        });
    }

    public sealed class PauseSendHandler : DelegatingHandler
    {
        public bool PauseSend { get; set; }

        public PauseSendHandler(HttpClientHandler handler) : base(handler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            while (PauseSend)
            {
                await Task.Delay(1);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
