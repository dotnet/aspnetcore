// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using System.Text;

namespace Microsoft.AspNetCore.WebSockets.Test;

public class SendReceiveTests
{
    [Fact]
    public async Task ClientToServerTextMessage()
    {
        const string message = "Hello, World!";

        var pair = WebSocketPair.Create();
        var sendBuffer = Encoding.UTF8.GetBytes(message);

        await pair.ClientSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);

        var receiveBuffer = new byte[32];
        var result = await pair.ServerSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        Assert.Equal(message, Encoding.UTF8.GetString(receiveBuffer, 0, result.Count));
    }

    [Fact]
    public async Task ServerToClientTextMessage()
    {
        const string message = "Hello, World!";

        var pair = WebSocketPair.Create();
        var sendBuffer = Encoding.UTF8.GetBytes(message);

        await pair.ServerSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);

        var receiveBuffer = new byte[32];
        var result = await pair.ClientSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        Assert.Equal(message, Encoding.UTF8.GetString(receiveBuffer, 0, result.Count));
    }

    [Fact]
    public async Task ClientToServerBinaryMessage()
    {
        var pair = WebSocketPair.Create();
        var sendBuffer = new byte[] { 0xde, 0xad, 0xbe, 0xef };

        await pair.ClientSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: CancellationToken.None);

        var receiveBuffer = new byte[32];
        var result = await pair.ServerSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

        Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
        Assert.Equal(sendBuffer, receiveBuffer.Take(result.Count).ToArray());
    }

    [Fact]
    public async Task ServerToClientBinaryMessage()
    {
        var pair = WebSocketPair.Create();
        var sendBuffer = new byte[] { 0xde, 0xad, 0xbe, 0xef };

        await pair.ServerSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: CancellationToken.None);

        var receiveBuffer = new byte[32];
        var result = await pair.ClientSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

        Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
        Assert.Equal(sendBuffer, receiveBuffer.Take(result.Count).ToArray());
    }

    [Fact]
    public async Task ThrowsWhenUnderlyingStreamClosed()
    {
        var pair = WebSocketPair.Create();
        var sendBuffer = new byte[] { 0xde, 0xad, 0xbe, 0xef };

        await pair.ServerSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: CancellationToken.None);

        var receiveBuffer = new byte[32];
        var result = await pair.ClientSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

        Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

        // Close the client socket's read end
        pair.ClientStream.ReadStream.End();

        // Assert.Throws doesn't support async :(
        try
        {
            await pair.ClientSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

            // The exception should prevent this line from running
            Assert.False(true, "Expected an exception to be thrown!");
        }
        catch (WebSocketException ex)
        {
            Assert.Equal(WebSocketError.ConnectionClosedPrematurely, ex.WebSocketErrorCode);
        }
    }
}
