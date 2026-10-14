// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
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

    [ConditionalFact]
    public async Task DuplicateWebSocketUpgradeHeader_Success()
    {
        // This test verifies that WebSocket upgrade requests with duplicate "websocket" values
        // in the Upgrade header (e.g., "Upgrade: websocket, websocket") are accepted.
        // This is a valid scenario per RFC 9110 which doesn't require header values to be distinct.
        if (Fixture.DeploymentParameters.HostingModel == HostingModel.InProcess)
        {
            // InProcess uses a different code path that already handles this correctly
            return;
        }

        Uri uri = new Uri(Fixture.DeploymentResult.ApplicationBaseUri + "WebSocketEcho");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using TcpClient client = new TcpClient();
        await client.ConnectAsync(uri.Host, uri.Port, timeout.Token);
        using var stream = new BufferedStream(client.GetStream());

        // Send WebSocket upgrade request with duplicate "websocket" in Upgrade header
        var secWebSocketKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        await SendWebSocketUpgradeRequestWithDuplicateHeader(stream, uri, secWebSocketKey, timeout.Token);

        string response = await ReadHttpResponseHeaders(stream, timeout.Token);
        string[] responseLines = response.Split("\r\n", StringSplitOptions.None);
        string[] statusLineParts = responseLines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        Assert.True(statusLineParts.Length >= 2, $"The response status line was invalid: {responseLines[0]}");
        Assert.Equal("HTTP/1.1", statusLineParts[0]);
        Assert.Equal(101, int.Parse(statusLineParts[1], CultureInfo.InvariantCulture));

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < responseLines.Length && responseLines[i].Length > 0; i++)
        {
            int separator = responseLines[i].IndexOf(':');
            Assert.True(separator > 0, $"The response header was invalid: {responseLines[i]}");

            string name = responseLines[i][..separator].Trim();
            string value = responseLines[i][(separator + 1)..].Trim();
            if (headers.TryGetValue(name, out string existingValue))
            {
                headers[name] = existingValue + ", " + value;
            }
            else
            {
                headers.Add(name, value);
            }
        }

        Assert.True(headers.ContainsKey("Connection"), "The response did not contain a Connection header.");
        Assert.True(ContainsHeaderToken(headers["Connection"], "Upgrade"), "The Connection header did not contain the Upgrade token.");
        Assert.True(headers.ContainsKey("Upgrade"), "The response did not contain an Upgrade header.");
        Assert.True(ContainsHeaderToken(headers["Upgrade"], "websocket"), "The Upgrade header did not contain the websocket token.");
        Assert.True(headers.ContainsKey("Sec-WebSocket-Accept"), "The response did not contain a Sec-WebSocket-Accept header.");

        string expectedAccept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(
            secWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
        Assert.Equal(expectedAccept, headers["Sec-WebSocket-Accept"]);

        using var webSocket = WebSocket.CreateFromStream(stream, isServer: false, subProtocol: null, keepAliveInterval: TimeSpan.FromMinutes(2));
        const string message = "duplicate websocket upgrade value";
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, timeout.Token);

        var receiveBuffer = new byte[1024];
        using var echoedMessage = new MemoryStream();
        WebSocketReceiveResult receiveResult;
        do
        {
            receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), timeout.Token);
            Assert.Equal(WebSocketMessageType.Text, receiveResult.MessageType);
            echoedMessage.Write(receiveBuffer, 0, receiveResult.Count);
        }
        while (!receiveResult.EndOfMessage);

        Assert.Equal(message, Encoding.UTF8.GetString(echoedMessage.ToArray()));

        await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "test complete", timeout.Token);
        WebSocketReceiveResult closeResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), timeout.Token);
        Assert.Equal(WebSocketMessageType.Close, closeResult.MessageType);
        Assert.Equal(WebSocketCloseStatus.NormalClosure, closeResult.CloseStatus);
        Assert.Equal(WebSocketState.Closed, webSocket.State);
    }

    private async Task SendWebSocketUpgradeRequestWithDuplicateHeader(Stream stream, Uri uri, string secWebSocketKey, CancellationToken cancellationToken)
    {
        // Send an HTTP/1.1 WebSocket upgrade request with duplicate "websocket" in Upgrade header
        StringBuilder builder = new StringBuilder();
        builder.Append("GET ");
        builder.Append(uri.PathAndQuery);
        builder.Append(" HTTP/1.1");
        builder.Append("\r\n");

        builder.Append("Host: ");
        builder.Append(uri.Host);
        builder.Append(':');
        builder.Append(uri.Port);
        builder.Append("\r\n");

        builder.Append("Connection: Upgrade\r\n");
        // Duplicate "websocket" values in Upgrade header - this is what we're testing
        builder.Append("Upgrade: websocket, websocket\r\n");
        builder.Append("Sec-WebSocket-Version: 13\r\n");
        builder.Append("Sec-WebSocket-Key: ");
        builder.Append(secWebSocketKey);
        builder.Append("\r\n");

        builder.Append("\r\n");
        var requestBytes = Encoding.ASCII.GetBytes(builder.ToString());
        await stream.WriteAsync(requestBytes, cancellationToken);
    }

    private static async Task<string> ReadHttpResponseHeaders(Stream stream, CancellationToken cancellationToken)
    {
        using var headers = new MemoryStream();
        var buffer = new byte[1];
        while (headers.Length < 16 * 1024)
        {
            int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                throw new EndOfStreamException("The response ended before its headers were complete.");
            }

            headers.WriteByte(buffer[0]);
            int length = (int)headers.Length;
            byte[] headerBytes = headers.GetBuffer();
            if (length >= 4 &&
                headerBytes[length - 4] == '\r' &&
                headerBytes[length - 3] == '\n' &&
                headerBytes[length - 2] == '\r' &&
                headerBytes[length - 1] == '\n')
            {
                return Encoding.ASCII.GetString(headerBytes, 0, length);
            }
        }

        throw new InvalidDataException("The response headers exceeded the maximum supported size.");
    }

    private static bool ContainsHeaderToken(string headerValue, string expectedToken)
    {
        foreach (string value in headerValue.Split(','))
        {
            if (string.Equals(value.Trim(), expectedToken, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
