// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

//------------------------------------------------------------------------------
// <copyright file="WebSocketHelpers.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Net.WebSockets;

namespace Microsoft.Net.Http.Server
{
    public static class WebSocketExtensions
    {
        public static bool IsWebSocketRequest(this RequestContext context)
        {
            if (!WebSocketHelpers.AreWebSocketsSupported)
            {
                return false;
            }

            if (!context.IsUpgradableRequest)
            {
                return false;
            }

            if (!string.Equals("GET", context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Connection: Upgrade (some odd clients send Upgrade,KeepAlive)
            string connection = context.Request.Headers[HttpKnownHeaderNames.Connection];
            if (connection == null || connection.IndexOf(HttpKnownHeaderNames.Upgrade, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            // Upgrade: websocket
            string upgrade = context.Request.Headers[HttpKnownHeaderNames.Upgrade];
            if (!string.Equals(WebSocketHelpers.WebSocketUpgradeToken, upgrade, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Sec-WebSocket-Version: 13
            string version = context.Request.Headers[HttpKnownHeaderNames.SecWebSocketVersion];
            if (!string.Equals(WebSocketConstants.SupportedProtocolVersion, version, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Sec-WebSocket-Key: {base64string}
            string key = context.Request.Headers[HttpKnownHeaderNames.SecWebSocketKey];
            if (!WebSocketHelpers.IsValidWebSocketKey(key))
            {
                return false;
            }

            return true;
        }

        // Compare IsWebSocketRequest()
        private static void ValidateWebSocketRequest(RequestContext context)
        {
            if (!WebSocketHelpers.AreWebSocketsSupported)
            {
                throw new NotSupportedException("WebSockets are not supported on this platform.");
            }

            if (!context.IsUpgradableRequest)
            {
                throw new InvalidOperationException("This request is not a valid upgrade request.");
            }

            if (!string.Equals("GET", context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("This request is not a valid upgrade request; invalid verb: " + context.Request.Method);
            }

            // Connection: Upgrade (some odd clients send Upgrade,KeepAlive)
            string connection = context.Request.Headers[HttpKnownHeaderNames.Connection];
            if (connection == null || connection.IndexOf(HttpKnownHeaderNames.Upgrade, StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException("The Connection header is invalid: " + connection);
            }

            // Upgrade: websocket
            string upgrade = context.Request.Headers[HttpKnownHeaderNames.Upgrade];
            if (!string.Equals(WebSocketHelpers.WebSocketUpgradeToken, upgrade, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The Upgrade header is invalid: " + upgrade);
            }

            // Sec-WebSocket-Version: 13
            string version = context.Request.Headers[HttpKnownHeaderNames.SecWebSocketVersion];
            if (!string.Equals(WebSocketConstants.SupportedProtocolVersion, version, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The Sec-WebSocket-Version header is invalid or not supported: " + version);
            }

            // Sec-WebSocket-Key: {base64string}
            string key = context.Request.Headers[HttpKnownHeaderNames.SecWebSocketKey];
            if (!WebSocketHelpers.IsValidWebSocketKey(key))
            {
                throw new InvalidOperationException("The Sec-WebSocket-Key header is invalid: " + upgrade);
            }
        }

        public static Task<WebSocket> AcceptWebSocketAsync(this RequestContext context)
        {
            return context.AcceptWebSocketAsync(null,
                WebSocketHelpers.DefaultReceiveBufferSize,
                WebSocketHelpers.DefaultKeepAliveInterval);
        }

        public static Task<WebSocket> AcceptWebSocketAsync(this RequestContext context, string subProtocol)
        {
            return context.AcceptWebSocketAsync(subProtocol,
                WebSocketHelpers.DefaultReceiveBufferSize,
                WebSocketHelpers.DefaultKeepAliveInterval);
        }

        public static Task<WebSocket> AcceptWebSocketAsync(this RequestContext context, string subProtocol, TimeSpan keepAliveInterval)
        {
            return context.AcceptWebSocketAsync(subProtocol,
                WebSocketHelpers.DefaultReceiveBufferSize,
                keepAliveInterval);
        }

        public static Task<WebSocket> AcceptWebSocketAsync(
            this RequestContext context,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval)
        {
            WebSocketHelpers.ValidateOptions(subProtocol, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, keepAliveInterval);

            ArraySegment<byte> internalBuffer = WebSocketBuffer.CreateInternalBufferArraySegment(receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);
            return context.AcceptWebSocketAsync(subProtocol,
                receiveBufferSize,
                keepAliveInterval,
                internalBuffer);
        }

        public static Task<WebSocket> AcceptWebSocketAsync(
            this RequestContext context,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            if (!context.IsUpgradableRequest)
            {
                throw new InvalidOperationException("This request is cannot be upgraded.");
            }
            WebSocketHelpers.ValidateOptions(subProtocol, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, keepAliveInterval);
            WebSocketHelpers.ValidateArraySegment<byte>(internalBuffer, "internalBuffer");
            WebSocketBuffer.Validate(internalBuffer.Count, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);

            return AcceptWebSocketAsyncCore(context, subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
        }

        private static async Task<WebSocket> AcceptWebSocketAsyncCore(
            RequestContext context,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            ValidateWebSocketRequest(context);

            var subProtocols = context.Request.Headers.GetValues(HttpKnownHeaderNames.SecWebSocketProtocol);
            bool shouldSendSecWebSocketProtocolHeader = WebSocketHelpers.ProcessWebSocketProtocolHeader(subProtocols, subProtocol);
            if (shouldSendSecWebSocketProtocolHeader)
            {
                context.Response.Headers[HttpKnownHeaderNames.SecWebSocketProtocol] = subProtocol;
            }

            // negotiate the websocket key return value
            string secWebSocketKey = context.Request.Headers[HttpKnownHeaderNames.SecWebSocketKey];
            string secWebSocketAccept = WebSocketHelpers.GetSecWebSocketAcceptString(secWebSocketKey);

            context.Response.Headers.Append(HttpKnownHeaderNames.Connection, HttpKnownHeaderNames.Upgrade);
            context.Response.Headers.Append(HttpKnownHeaderNames.Upgrade, WebSocketHelpers.WebSocketUpgradeToken);
            context.Response.Headers.Append(HttpKnownHeaderNames.SecWebSocketAccept, secWebSocketAccept);

            Stream opaqueStream = await context.UpgradeAsync();

            return WebSocketHelpers.CreateServerWebSocket(
                opaqueStream,
                subProtocol,
                receiveBufferSize,
                keepAliveInterval,
                internalBuffer);
        }
    }
}
