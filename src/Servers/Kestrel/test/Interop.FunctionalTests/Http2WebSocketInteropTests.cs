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
}
