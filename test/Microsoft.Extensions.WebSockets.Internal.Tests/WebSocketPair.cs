// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    internal class WebSocketPair : IDisposable
    {
        private PipelineFactory _factory;

        public PipelineReaderWriter ServerToClient { get; }
        public PipelineReaderWriter ClientToServer { get; }

        public IWebSocketConnection ClientSocket { get; }
        public IWebSocketConnection ServerSocket { get; }

        public WebSocketPair(PipelineFactory factory, PipelineReaderWriter serverToClient, PipelineReaderWriter clientToServer, IWebSocketConnection clientSocket, IWebSocketConnection serverSocket)
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
            var factory = new PipelineFactory();
            var serverToClient = factory.Create();
            var clientToServer = factory.Create();

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