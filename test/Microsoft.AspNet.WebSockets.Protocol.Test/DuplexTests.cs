// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.WebSockets.Protocol.Test
{
    public class DuplexTests
    {
        [Fact]
        public async Task SendAndReceive()
        {
            DuplexStream serverStream = new DuplexStream();
            DuplexStream clientStream = serverStream.CreateReverseDuplexStream();

            WebSocket serverWebSocket = CommonWebSocket.CreateServerWebSocket(serverStream, null, TimeSpan.FromMinutes(2), 1024);
            WebSocket clientWebSocket = CommonWebSocket.CreateClientWebSocket(clientStream, null, TimeSpan.FromMinutes(2), 1024, false);

            byte[] clientBuffer = Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz");
            byte[] serverBuffer = new byte[clientBuffer.Length];

            await clientWebSocket.SendAsync(new ArraySegment<byte>(clientBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            WebSocketReceiveResult serverResult = await serverWebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
            Assert.True(serverResult.EndOfMessage);
            Assert.Equal(clientBuffer.Length, serverResult.Count);
            Assert.Equal(WebSocketMessageType.Text, serverResult.MessageType);
            Assert.Equal(clientBuffer, serverBuffer);
        }

        [Fact]
        // Tests server unmasking with offset masks
        public async Task ServerReceiveOffsetData()
        {
            DuplexStream serverStream = new DuplexStream();
            DuplexStream clientStream = serverStream.CreateReverseDuplexStream();

            WebSocket serverWebSocket = CommonWebSocket.CreateServerWebSocket(serverStream, null, TimeSpan.FromMinutes(2), 1024);
            WebSocket clientWebSocket = CommonWebSocket.CreateClientWebSocket(clientStream, null, TimeSpan.FromMinutes(2), 1024, false);

            byte[] clientBuffer = Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz");
            byte[] serverBuffer = new byte[clientBuffer.Length];

            await clientWebSocket.SendAsync(new ArraySegment<byte>(clientBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            WebSocketReceiveResult serverResult = await serverWebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer, 0, 3), CancellationToken.None);
            Assert.False(serverResult.EndOfMessage);
            Assert.Equal(3, serverResult.Count);
            Assert.Equal(WebSocketMessageType.Text, serverResult.MessageType);

            serverResult = await serverWebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer, 3, 10), CancellationToken.None);
            Assert.False(serverResult.EndOfMessage);
            Assert.Equal(10, serverResult.Count);
            Assert.Equal(WebSocketMessageType.Text, serverResult.MessageType);
            
            serverResult = await serverWebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer, 13, 13), CancellationToken.None);
            Assert.True(serverResult.EndOfMessage);
            Assert.Equal(13, serverResult.Count);
            Assert.Equal(WebSocketMessageType.Text, serverResult.MessageType);
            Assert.Equal(clientBuffer, serverBuffer);
        }
    }
}
