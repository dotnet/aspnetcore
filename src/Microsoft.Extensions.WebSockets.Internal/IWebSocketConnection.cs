// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.WebSockets.Internal
{
    /// <summary>
    /// Represents a connection to a WebSocket endpoint.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementors of this type are generally considered thread-safe under the following condition: No two threads attempt to call either
    /// <see cref="ExecuteAsync"/> or <see cref="SendAsync"/> simultaneously. Different threads may call each method, but the same method
    /// cannot be re-entered while it is being run in a different thread. However, ensure you verify that the specific implementor is
    /// thread-safe in this way. For example, <see cref="WebSocketConnection"/> (including the implementations returned by the
    /// static factory methods on that type) is thread-safe in this way.
    /// </para>
    /// <para>
    /// The general pattern of having a single thread running <see cref="ExecuteAsync"/> and a separate thread running <see cref="SendAsync"/> will
    /// be thread-safe, as each method interacts with completely separate state.
    /// </para>
    /// </remarks>
    public interface IWebSocketConnection : IDisposable
    {
        /// <summary>
        /// Gets the sub-protocol value configured during handshaking.
        /// </summary>
        string SubProtocol { get; }

        /// <summary>
        /// Gets the current state of the connection
        /// </summary>
        WebSocketConnectionState State { get; }

        /// <summary>
        /// Sends the specified frame.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the send is cancelled.</param>
        /// <returns>A <see cref="Task"/> that completes when the message has been written to the outbound stream.</returns>
        Task SendAsync(WebSocketFrame message, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a Close frame to the other party. This does not guarantee that the client will send a responding close frame.
        /// </summary>
        /// <remarks>
        /// If the other party does not respond with a close frame, the connection will remain open and the <see cref="Task{WebSocketCloseResult}"/>
        /// will remain active. Call the <see cref="IDisposable.Dispose"/> method on this instance to forcibly terminate the connection.
        /// </remarks>
        /// <param name="result">A <see cref="WebSocketCloseResult"/> with the payload for the close frame</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the send is cancelled.</param>
        /// <returns>A <see cref="Task"/> that completes when the close frame has been sent</returns>
        Task CloseAsync(WebSocketCloseResult result, CancellationToken cancellationToken);

        /// <summary>
        /// Runs the WebSocket receive loop, using the provided message handler. Note that <see cref="WebSocketOpcode.Ping"/> and
        /// <see cref="WebSocketOpcode.Pong"/> frames will be passed to this handler for tracking/logging/monitoring, BUT will automatically be handled.
        /// </summary>
        /// <param name="messageHandler">The callback that will be invoked for each new frame</param>
        /// <param name="state">A state parameter that will be passed to each invocation of <paramref name="messageHandler"/></param>
        /// <returns>A <see cref="Task{WebSocketCloseResult}"/> that will complete when the client has sent a close frame, or the connection has been terminated</returns>
        Task<WebSocketCloseResult> ExecuteAsync(Func<WebSocketFrame, object, Task> messageHandler, object state);

        /// <summary>
        /// Forcibly terminates the socket, cleaning up the necessary resources.
        /// </summary>
        void Abort();
    }

    public static class WebSocketConnectionExtensions
    {
        /// <summary>
        /// Sends the specified frame.
        /// </summary>
        /// <param name="connection">The <see cref="IWebSocketConnection"/></param>
        /// <param name="message">The message to send.</param>
        /// <returns>A <see cref="Task"/> that completes when the message has been written to the outbound stream.</returns>
        public static Task SendAsync(this IWebSocketConnection connection, WebSocketFrame message) => connection.SendAsync(message, CancellationToken.None);

        /// <summary>
        /// Sends a Close frame to the other party. This does not guarantee that the client will send a responding close frame.
        /// </summary>
        /// <param name="connection">The <see cref="IWebSocketConnection"/></param>
        /// <param name="status">A <see cref="WebSocketCloseStatus"/> value to be sent to the client in the close frame</param>.
        /// <returns>A <see cref="Task"/> that completes when the close frame has been sent</returns>
        public static Task CloseAsync(this IWebSocketConnection connection, WebSocketCloseStatus status) => connection.CloseAsync(new WebSocketCloseResult(status), CancellationToken.None);

        /// <summary>
        /// Sends a Close frame to the other party. This does not guarantee that the client will send a responding close frame.
        /// </summary>
        /// <param name="connection">The <see cref="IWebSocketConnection"/></param>
        /// <param name="status">A <see cref="WebSocketCloseStatus"/> value to be sent to the client in the close frame</param>.
        /// <param name="description">A textual description of the reason for closing the connection.</param>
        /// <returns>A <see cref="Task"/> that completes when the close frame has been sent</returns>
        public static Task CloseAsync(this IWebSocketConnection connection, WebSocketCloseStatus status, string description) => connection.CloseAsync(new WebSocketCloseResult(status, description), CancellationToken.None);

        /// <summary>
        /// Sends a Close frame to the other party. This does not guarantee that the client will send a responding close frame.
        /// </summary>
        /// <param name="connection">The <see cref="IWebSocketConnection"/></param>
        /// <param name="status">A <see cref="WebSocketCloseStatus"/> value to be sent to the client in the close frame</param>.
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the send is cancelled.</param>
        /// <returns>A <see cref="Task"/> that completes when the close frame has been sent</returns>
        public static Task CloseAsync(this IWebSocketConnection connection, WebSocketCloseStatus status, CancellationToken cancellationToken) => connection.CloseAsync(new WebSocketCloseResult(status), cancellationToken);

        /// <summary>
        /// Sends a Close frame to the other party. This does not guarantee that the client will send a responding close frame.
        /// </summary>
        /// <param name="connection">The <see cref="IWebSocketConnection"/></param>
        /// <param name="status">A <see cref="WebSocketCloseStatus"/> value to be sent to the client in the close frame</param>.
        /// <param name="description">A textual description of the reason for closing the connection.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the send is cancelled.</param>
        /// <returns>A <see cref="Task"/> that completes when the close frame has been sent</returns>
        public static Task CloseAsync(this IWebSocketConnection connection, WebSocketCloseStatus status, string description, CancellationToken cancellationToken) => connection.CloseAsync(new WebSocketCloseResult(status, description), cancellationToken);

        /// <summary>
        /// Sends a Close frame to the other party. This does not guarantee that the client will send a responding close frame.
        /// </summary>
        /// <param name="connection">The <see cref="IWebSocketConnection"/></param>
        /// <param name="result">A <see cref="WebSocketCloseResult"/> with the payload for the close frame.</param>
        /// <returns>A <see cref="Task"/> that completes when the close frame has been sent</returns>
        public static Task CloseAsync(this IWebSocketConnection connection, WebSocketCloseResult result) => connection.CloseAsync(result, CancellationToken.None);

        /// <summary>
        /// Runs the WebSocket receive loop, using the provided message handler.
        /// </summary>
        /// <param name="connection">The <see cref="IWebSocketConnection"/></param>
        /// <param name="messageHandler">The callback that will be invoked for each new frame</param>
        /// <returns>A <see cref="Task{WebSocketCloseResult}"/> that will complete when the client has sent a close frame, or the connection has been terminated</returns>
        public static Task<WebSocketCloseResult> ExecuteAsync(this IWebSocketConnection connection, Action<WebSocketFrame> messageHandler) =>
            connection.ExecuteAsync((frame, _) =>
            {
                messageHandler(frame);
                return Task.CompletedTask;
            }, null);

        /// <summary>
        /// Runs the WebSocket receive loop, using the provided message handler.
        /// </summary>
        /// <param name="connection">The <see cref="IWebSocketConnection"/></param>
        /// <param name="messageHandler">The callback that will be invoked for each new frame</param>
        /// <param name="state">The state to pass to the callback when the delegate is invoked. This may be null.</param>
        /// <returns>A <see cref="Task{WebSocketCloseResult}"/> that will complete when the client has sent a close frame, or the connection has been terminated</returns>
        public static Task<WebSocketCloseResult> ExecuteAsync(this IWebSocketConnection connection, Action<WebSocketFrame, object> messageHandler, object state) =>
            connection.ExecuteAsync((frame, s) =>
            {
                messageHandler(frame, s);
                return Task.CompletedTask;
            }, state);

        /// <summary>
        /// Runs the WebSocket receive loop, using the provided message handler.
        /// </summary>
        /// <param name="connection">The <see cref="IWebSocketConnection"/></param>
        /// <param name="messageHandler">The callback that will be invoked for each new frame</param>
        /// <returns>A <see cref="Task{WebSocketCloseResult}"/> that will complete when the client has sent a close frame, or the connection has been terminated</returns>
        public static Task<WebSocketCloseResult> ExecuteAsync(this IWebSocketConnection connection, Func<WebSocketFrame, Task> messageHandler) =>
            connection.ExecuteAsync((frame, _) => messageHandler(frame), null);
    }
}
