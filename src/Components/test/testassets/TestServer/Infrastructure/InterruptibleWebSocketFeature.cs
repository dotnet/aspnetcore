using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Components.TestServer
{
    public class InterruptibleWebSocketFeature : IHttpWebSocketFeature
    {
        public InterruptibleWebSocketFeature(
            IHttpWebSocketFeature socketsFeature,
            string socketIdentifier,
            ConcurrentDictionary<string, InterruptibleWebSocket> registry)
        {
            OriginalFeature = socketsFeature;
            SocketIdentifier = socketIdentifier;
            Registry = registry;
        }

        public bool IsWebSocketRequest => OriginalFeature.IsWebSocketRequest;

        public string SocketIdentifier { get; }

        private IHttpWebSocketFeature OriginalFeature { get; }

        public ConcurrentDictionary<string, InterruptibleWebSocket> Registry { get; }

        public async Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
        {
            var socket = new InterruptibleWebSocket(await OriginalFeature.AcceptAsync(context), SocketIdentifier);
            return Registry.AddOrUpdate(SocketIdentifier, socket, (k, e) =>
            {
                try
                {
                    e.Dispose();
                }
                catch (Exception)
                {
                }

                return socket;
            });
        }
    }
}
