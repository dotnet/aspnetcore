// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Channels;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    internal class WebSocketPair : IDisposable
    {
        private ChannelFactory _factory;

        public Channel ServerToClient { get; }
        public Channel ClientToServer { get; }

        public IWebSocketConnection ClientSocket { get; }
        public IWebSocketConnection ServerSocket { get; }

        public WebSocketPair(ChannelFactory factory, Channel serverToClient, Channel clientToServer, IWebSocketConnection clientSocket, IWebSocketConnection serverSocket)
        {
            _factory = factory;
            ServerToClient = serverToClient;
            ClientToServer = clientToServer;
            ClientSocket = clientSocket;
            ServerSocket = serverSocket;
        }

        public static WebSocketPair Create() => Create(new WebSocketOptions().WithAllFramesPassedThrough().WithRandomMasking(), new WebSocketOptions().WithAllFramesPassedThrough());

        public static WebSocketPair Create(WebSocketOptions serverOptions, WebSocketOptions clientOptions)
        {
            // Create channels
            var factory = new ChannelFactory();
            var serverToClient = factory.CreateChannel();
            var clientToServer = factory.CreateChannel();

            var serverSocket = new WebSocketConnection(clientToServer, serverToClient, options: serverOptions);
            var clientSocket = new WebSocketConnection(serverToClient, clientToServer, options: clientOptions);

            return new WebSocketPair(factory, serverToClient, clientToServer, clientSocket, serverSocket);
        }

        public void Dispose()
        {
            ServerSocket.Dispose();
            ClientSocket.Dispose();
            _factory.Dispose();
        }

        public void TerminateFromClient(Exception ex = null)
        {
            ClientToServer.CompleteWriter(ex);
        }
    }
}