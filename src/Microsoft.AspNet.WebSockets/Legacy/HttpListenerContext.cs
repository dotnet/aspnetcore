//------------------------------------------------------------------------------
// <copyright file="HttpListenerContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
/*
namespace Microsoft.Net
{
    using Microsoft.AspNet.WebSockets;
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;

    public sealed unsafe class HttpListenerContext  {
        private HttpListenerRequest m_Request;

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol)
        {
            return this.AcceptWebSocketAsync(subProtocol, 
                WebSocketHelpers.DefaultReceiveBufferSize,
                WebSocket.DefaultKeepAliveInterval);
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, TimeSpan keepAliveInterval)
        {
            return this.AcceptWebSocketAsync(subProtocol,
                WebSocketHelpers.DefaultReceiveBufferSize,
                keepAliveInterval);
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, 
            int receiveBufferSize, 
            TimeSpan keepAliveInterval)
        {
            WebSocketHelpers.ValidateOptions(subProtocol, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, keepAliveInterval);

            ArraySegment<byte> internalBuffer = WebSocketBuffer.CreateInternalBufferArraySegment(receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);
            return this.AcceptWebSocketAsync(subProtocol, 
                receiveBufferSize, 
                keepAliveInterval, 
                internalBuffer);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, 
            int receiveBufferSize, 
            TimeSpan keepAliveInterval, 
            ArraySegment<byte> internalBuffer)
        {
            return WebSocketHelpers.AcceptWebSocketAsync(this, 
                subProtocol, 
                receiveBufferSize, 
                keepAliveInterval, 
                internalBuffer);
        }        
    }
}
*/