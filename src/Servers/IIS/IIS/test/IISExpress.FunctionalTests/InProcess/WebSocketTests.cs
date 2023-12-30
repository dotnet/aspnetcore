// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.IISExpress.FunctionalTests;

[Collection(IISTestSiteCollection.Name)]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "No WebSocket supported on Win7")]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class WebSocketsTests
{
    private readonly string _requestUri;
    private readonly string _webSocketUri;

    public WebSocketsTests(IISTestSiteFixture fixture)
    {
        _requestUri = fixture.DeploymentResult.ApplicationBaseUri;
        _webSocketUri = _requestUri.Replace("http:", "ws:");
    }

    [ConditionalFact]
    public async Task RequestWithBody_NotUpgradable()
    {
        using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(200) };
        using var response = await client.PostAsync(_requestUri + "WebSocketNotUpgradable", new StringContent("Hello World"));
        response.EnsureSuccessStatusCode();
    }

    [ConditionalFact]
    public async Task RequestWithoutBody_Upgradable()
    {
        using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(200) };
        // POST with Content-Length: 0 counts as not having a body.
        using var response = await client.PostAsync(_requestUri + "WebSocketUpgradable", new StringContent(""));
        response.EnsureSuccessStatusCode();
    }

    [ConditionalFact]
    public async Task OnStartedCalledForWebSocket()
    {
        var cws = new ClientWebSocket();
        await cws.ConnectAsync(new Uri(_webSocketUri + "WebSocketLifetimeEvents"), default);

        await ReceiveMessage(cws, "OnStarting");
        await ReceiveMessage(cws, "Upgraded");
    }

    [ConditionalFact]
    public async Task WebReadBeforeUpgrade()
    {
        var cws = new ClientWebSocket();
        await cws.ConnectAsync(new Uri(_webSocketUri + "WebSocketReadBeforeUpgrade"), default);

        await ReceiveMessage(cws, "Yay");
    }

    [ConditionalFact]
    public async Task CanSendAndReceieveData()
    {
        var cws = new ClientWebSocket();
        await cws.ConnectAsync(new Uri(_webSocketUri + "WebSocketEcho"), default);

        for (int i = 0; i < 1000; i++)
        {
            var mesage = i.ToString(CultureInfo.InvariantCulture);
            await SendMessage(cws, mesage);
            await ReceiveMessage(cws, mesage);
        }
    }

    [ConditionalFact]
    public async Task Http1_0_Request_NotUpgradable()
    {
        Uri uri = new Uri(_requestUri + "WebSocketNotUpgradable");
        using TcpClient client = new TcpClient();

        await client.ConnectAsync(uri.Host, uri.Port);
        NetworkStream stream = client.GetStream();

        await SendHttp10Request(stream, uri);

        StreamReader reader = new StreamReader(stream);
        string statusLine = await reader.ReadLineAsync();
        string[] parts = statusLine.Split(' ');
        if (int.Parse(parts[1], CultureInfo.InvariantCulture) != 200)
        {
            throw new InvalidOperationException("The response status code was incorrect: " + statusLine);
        }
    }

    [ConditionalFact]
    public async Task Http1_0_Request_UpgradeErrors()
    {
        Uri uri = new Uri(_requestUri + "WebSocketUpgradeFails");
        using TcpClient client = new TcpClient();

        await client.ConnectAsync(uri.Host, uri.Port);
        NetworkStream stream = client.GetStream();

        await SendHttp10Request(stream, uri);

        StreamReader reader = new StreamReader(stream);
        string statusLine = await reader.ReadLineAsync();
        string[] parts = statusLine.Split(' ');
        if (int.Parse(parts[1], CultureInfo.InvariantCulture) != 200)
        {
            throw new InvalidOperationException("The response status code was incorrect: " + statusLine);
        }
    }

    private async Task SendHttp10Request(NetworkStream stream, Uri uri)
    {
        // Send an HTTP GET request
        StringBuilder builder = new StringBuilder();
        builder.Append("GET");
        builder.Append(" ");
        builder.Append(uri.PathAndQuery);
        builder.Append(" HTTP/1.0");
        builder.AppendLine();

        builder.Append("Host: ");
        builder.Append(uri.Host);
        builder.Append(':');
        builder.Append(uri.Port);
        builder.AppendLine();

        builder.AppendLine();
        var requestBytes = Encoding.ASCII.GetBytes(builder.ToString());
        await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
    }

    private async Task SendMessage(ClientWebSocket webSocket, string message)
    {
        await webSocket.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(message)), WebSocketMessageType.Text, true, default);
    }

    private async Task ReceiveMessage(ClientWebSocket webSocket, string expectedMessage)
    {
        var received = new byte[expectedMessage.Length];

        var offset = 0;
        WebSocketReceiveResult result;
        do
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(received, offset, received.Length - offset), default);
            offset += result.Count;
        } while (!result.EndOfMessage);

        Assert.Equal(expectedMessage, Encoding.ASCII.GetString(received));
    }
}
