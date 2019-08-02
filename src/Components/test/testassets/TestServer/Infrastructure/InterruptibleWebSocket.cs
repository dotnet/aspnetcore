using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Components.TestServer
{
    // A socket used in testing that wraps the underlying websocket and that has built-in hooks to allow
    // tests to start generating exceptions at will to test scenarios where the websocket connection gets
    // closed in a non-graceful way.
    public class InterruptibleWebSocket : WebSocket
    {
        private TaskCompletionSource<WebSocketReceiveResult> _disabledReceiveTask =
            new TaskCompletionSource<WebSocketReceiveResult>();

        public InterruptibleWebSocket(WebSocket socket, string identifier)
        {
            Socket = socket;
            Identifier = identifier;
        }

        public WebSocket Socket { get; }
        public string Identifier { get; }

        public override WebSocketCloseStatus? CloseStatus => Socket.CloseStatus;

        public override string CloseStatusDescription => Socket.CloseStatusDescription;

        public override WebSocketState State => Socket.State;

        public override string SubProtocol => Socket.SubProtocol;

        public override void Abort()
        {
            Socket.Abort();
        }

        public void Disable()
        {
            _disabledReceiveTask.TrySetException(new IOException($"Socket {Identifier} got disabled by the test."));
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return Socket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return Socket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override void Dispose()
        {
            Socket.Dispose();
        }

        // Consumers will call ReceiveAsync to wait for data from the network to come through.
        // We have setup this websocket so that we can trigger errors from outside. When we get
        // notified that we need to create errors, we simply return an exception to the caller
        // on the current call and successive calls so that the server believes the connection
        // got disconnected abruptly.
        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (_disabledReceiveTask.Task.IsCompleted)
            {
                return await _disabledReceiveTask.Task;
            }
            else
            {
                return await Task.WhenAny(
                    _disabledReceiveTask.Task,
                    Socket.ReceiveAsync(buffer, cancellationToken)).Unwrap();
            }
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return Socket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }
    }
}
