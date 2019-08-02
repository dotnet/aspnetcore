using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace Components.TestServer
{
    public class InterruptibleWebSocketOptions
    {
        public PathString WebSocketPath { get; set; } = new PathString("/_blazor");
        public PathString InterruptPath { get; set; } = new PathString("/WebSockets/Interrupt");

        public string WebSocketIdParameterName { get; set; } = "WebSockets.Identifier";
        public ConcurrentDictionary<string, InterruptibleWebSocket> Registry { get; set; } =
            new ConcurrentDictionary<string, InterruptibleWebSocket>();
    }
}
