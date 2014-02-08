//------------------------------------------------------------------------------
// <copyright file="WebSocket.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.WebSockets
{
    public abstract class WebSocket : IDisposable
    {
        private static TimeSpan? defaultKeepAliveInterval;

        public abstract WebSocketCloseStatus? CloseStatus { get; }
        public abstract string CloseStatusDescription { get; }
        public abstract string SubProtocol { get; }
        public abstract WebSocketState State { get; }

        public static TimeSpan DefaultKeepAliveInterval
        {
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
                Justification = "This is a harmless read-only operation")]
            get
            {
                if (defaultKeepAliveInterval == null)
                {
                    if (UnsafeNativeMethods.WebSocketProtocolComponent.IsSupported)
                    {
                        defaultKeepAliveInterval = UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketGetDefaultKeepAliveInterval();
                    }
                    else
                    {
                        defaultKeepAliveInterval = Timeout.InfiniteTimeSpan;
                    }
                }
                return defaultKeepAliveInterval.Value;
            }
        }

        public static ArraySegment<byte> CreateClientBuffer(int receiveBufferSize, int sendBufferSize)
        {
            WebSocketHelpers.ValidateBufferSizes(receiveBufferSize, sendBufferSize);

            return WebSocketBuffer.CreateInternalBufferArraySegment(receiveBufferSize, sendBufferSize, false);
        }

        public static ArraySegment<byte> CreateServerBuffer(int receiveBufferSize)
        {
            WebSocketHelpers.ValidateBufferSizes(receiveBufferSize, WebSocketBuffer.MinSendBufferSize);

            return WebSocketBuffer.CreateInternalBufferArraySegment(receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);
        }

        internal static WebSocket CreateServerWebSocket(Stream innerStream,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            if (!UnsafeNativeMethods.WebSocketProtocolComponent.IsSupported)
            {
                WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
            }

            WebSocketHelpers.ValidateInnerStream(innerStream);
            WebSocketHelpers.ValidateOptions(subProtocol, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, keepAliveInterval);
            WebSocketHelpers.ValidateArraySegment<byte>(internalBuffer, "internalBuffer");
            WebSocketBuffer.Validate(internalBuffer.Count, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);

            return new ServerWebSocket(innerStream, 
                subProtocol, 
                receiveBufferSize,
                keepAliveInterval, 
                internalBuffer);
        }

        public abstract void Abort();
        public abstract Task CloseAsync(WebSocketCloseStatus closeStatus, 
            string statusDescription, 
            CancellationToken cancellationToken);
        public abstract Task CloseOutputAsync(WebSocketCloseStatus closeStatus, 
            string statusDescription, 
            CancellationToken cancellationToken);
        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "This rule is outdated")]
        public abstract void Dispose();
        public abstract Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, 
            CancellationToken cancellationToken);
        public abstract Task SendAsync(ArraySegment<byte> buffer, 
            WebSocketMessageType messageType, 
            bool endOfMessage, 
            CancellationToken cancellationToken);

        protected static void ThrowOnInvalidState(WebSocketState state, params WebSocketState[] validStates)
        {
            string validStatesText = string.Empty;

            if (validStates != null && validStates.Length > 0)
            {
                foreach (WebSocketState currentState in validStates)
                {
                    if (state == currentState)
                    {
                        return;
                    }
                }

                validStatesText = string.Join(", ", validStates);
            }

            throw new WebSocketException(SR.GetString(SR.net_WebSockets_InvalidState, state, validStatesText));
        }

        protected static bool IsStateTerminal(WebSocketState state)
        {
            return state == WebSocketState.Closed ||
                   state == WebSocketState.Aborted;
        }
    }
}
