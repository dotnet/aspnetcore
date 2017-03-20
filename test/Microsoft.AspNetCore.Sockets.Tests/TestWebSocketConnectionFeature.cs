using System;
using System.Buffers.Pools;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets.Internal;
using Microsoft.Extensions.WebSockets.Internal;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    internal class TestWebSocketConnectionFeature : IHttpWebSocketConnectionFeature, IDisposable
    {
        private PipeFactory _factory = new PipeFactory(ManagedBufferPool.Shared);

        public bool IsWebSocketRequest => true;

        public WebSocketConnection Client { get; private set; }

        public ValueTask<IWebSocketConnection> AcceptWebSocketConnectionAsync(WebSocketAcceptContext context)
        {
            var clientToServer = _factory.Create();
            var serverToClient = _factory.Create();

            var clientSocket = new WebSocketConnection(serverToClient.Reader, clientToServer.Writer);
            var serverSocket = new WebSocketConnection(clientToServer.Reader, serverToClient.Writer);

            Client = clientSocket;
            return new ValueTask<IWebSocketConnection>(serverSocket);
        }

        public void Dispose()
        {
            _factory.Dispose();
        }
    }
}