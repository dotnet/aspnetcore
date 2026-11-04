// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Xunit.Abstractions;

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

public abstract class WebSocketsTests : FunctionalTestsBase
{
    public IISTestSiteFixture Fixture { get; }

    public WebSocketsTests(IISTestSiteFixture fixture, ITestOutputHelper testOutput) : base(testOutput)
    {
        Fixture = fixture;
    }

    [ConditionalFact]
    public async Task RequestWithBody_NotUpgradable()
    {
        using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(200) };
        using var response = await client.PostAsync(Fixture.DeploymentResult.ApplicationBaseUri + "WebSocketNotUpgradable", new StringContent("Hello World"));
        response.EnsureSuccessStatusCode();
    }

    [ConditionalFact]
    public async Task RequestWithoutBody_Upgradable()
    {
        if (Fixture.DeploymentParameters.HostingModel == HostingModel.OutOfProcess)
        {
            // OutOfProcess doesn't support upgrade requests without the "Upgrade": "websocket" header.
            return;
        }

        using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(200) };
        // POST with Content-Length: 0 counts as not having a body.
        using var response = await client.PostAsync(Fixture.DeploymentResult.ApplicationBaseUri + "WebSocketUpgradable", new StringContent(""));
        response.EnsureSuccessStatusCode();
    }

    [ConditionalFact]
    public async Task OnStartedCalledForWebSocket()
    {
        var webSocketUri = Fixture.DeploymentResult.ApplicationBaseUri;
        webSocketUri = webSocketUri.Replace("http:", "ws:");

        using var cws = new ClientWebSocket();
        await cws.ConnectAsync(new Uri(webSocketUri + "WebSocketLifetimeEvents"), default);

        await ReceiveMessage(cws, "OnStarting");
        await ReceiveMessage(cws, "Upgraded");
    }

    [ConditionalFact]
    public async Task WebReadBeforeUpgrade()
    {
        var webSocketUri = Fixture.DeploymentResult.ApplicationBaseUri;
        webSocketUri = webSocketUri.Replace("http:", "ws:");

        using var cws = new ClientWebSocket();
        await cws.ConnectAsync(new Uri(webSocketUri + "WebSocketReadBeforeUpgrade"), default);

        await ReceiveMessage(cws, "Yay");
    }

    [ConditionalFact]
    public async Task CanSendAndReceieveData()
    {
        var webSocketUri = Fixture.DeploymentResult.ApplicationBaseUri;
        webSocketUri = webSocketUri.Replace("http:", "ws:");

        using var cws = new ClientWebSocket();
        await cws.ConnectAsync(new Uri(webSocketUri + "WebSocketEcho"), default);

        for (int i = 0; i < 1000; i++)
        {
            var mesage = i.ToString(CultureInfo.InvariantCulture);
            await SendMessage(cws, mesage);
            await ReceiveMessage(cws, mesage);
        }
    }

    [ConditionalFact]
    public async Task AttemptCompressionWorks()
    {
        var webSocketUri = Fixture.DeploymentResult.ApplicationBaseUri;
        webSocketUri = webSocketUri.Replace("http:", "ws:");

        using var cws = new ClientWebSocket();
        cws.Options.DangerousDeflateOptions = new WebSocketDeflateOptions();
        await cws.ConnectAsync(new Uri(webSocketUri + "WebSocketAllowCompression"), default);

        // Compression doesn't work with OutOfProcess, let's make sure the websocket extensions aren't forwarded and the connection still works
        var expected = Fixture.DeploymentParameters.HostingModel == HostingModel.InProcess
            ? "permessage-deflate; client_max_window_bits=15" : "None";
        await ReceiveMessage(cws, expected);

        for (int i = 0; i < 1000; i++)
        {
            var message = i.ToString(CultureInfo.InvariantCulture);
            await SendMessage(cws, message);
            await ReceiveMessage(cws, message);
        }
    }

    [ConditionalFact]
    public async Task Http1_0_Request_NotUpgradable()
    {
        Uri uri = new Uri(Fixture.DeploymentResult.ApplicationBaseUri + "WebSocketNotUpgradable");
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
        Uri uri = new Uri(Fixture.DeploymentResult.ApplicationBaseUri + "WebSocketUpgradeFails");
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
        Debug.Assert(expectedMessage.Length > 0);
        var received = new byte[expectedMessage.Length];

        var offset = 0;
        WebSocketReceiveResult result;
        do
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(received, offset, received.Length - offset), default);
            offset += result.Count;
        } while (!result.EndOfMessage && result.CloseStatus is null && received.Length - offset > 0);

        Assert.Equal(expectedMessage, Encoding.ASCII.GetString(received));
    }
}
