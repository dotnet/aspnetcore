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
/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Microsoft.AspNet.WebSockets
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using OpaqueUpgrade =
        Action
        <
            IDictionary<string, object>, // Parameters
            Func // OpaqueFunc callback
            <
                IDictionary<string, object>, // Opaque environment
                Task // Complete
            >
        >;
    using WebSocketAccept =
        Action
        <
            IDictionary<string, object>, // WebSocket Accept parameters
            Func // WebSocketFunc callback
            <
                IDictionary<string, object>, // WebSocket environment
                Task // Complete
            >
        >;
    using WebSocketFunc =
        Func
        <
            IDictionary<string, object>, // WebSocket Environment
            Task // Complete
        >;

    public class WebSocketMiddleware
    {
        private AppFunc _next;

        public WebSocketMiddleware(AppFunc next)
        {
            _next = next;
        }

        public Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            // Detect if an opaque upgrade is available, and if websocket upgrade headers are present.
            // If so, add a websocket upgrade.  
            OpaqueUpgrade upgrade = context.Get<OpaqueUpgrade>("opaque.Upgrade");
            if (upgrade != null)
            {
                // Headers and values:
                // Connection: Upgrade
                // Upgrade: WebSocket
                // Sec-WebSocket-Version: (WebSocketProtocolComponent.SupportedVersion)
                // Sec-WebSocket-Key: (hash, see WebSocketHelpers.GetSecWebSocketAcceptString)
                // Sec-WebSocket-Protocol: (optional, list)
                IList<string> connectionHeaders = context.Request.Headers.GetCommaSeparatedValues(HttpKnownHeaderNames.Connection); // "Upgrade, KeepAlive"
                string upgradeHeader = context.Request.Headers[HttpKnownHeaderNames.Upgrade];
                string versionHeader = context.Request.Headers[HttpKnownHeaderNames.SecWebSocketVersion];
                string keyHeader = context.Request.Headers[HttpKnownHeaderNames.SecWebSocketKey];

                if (connectionHeaders != null && connectionHeaders.Count > 0
                    && connectionHeaders.Contains(HttpKnownHeaderNames.Upgrade, StringComparer.OrdinalIgnoreCase)
                    && string.Equals(upgradeHeader, WebSocketHelpers.WebSocketUpgradeToken, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(versionHeader, UnsafeNativeMethods.WebSocketProtocolComponent.SupportedVersion, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(keyHeader))
                {
                    environment["websocket.Accept"] = new WebSocketAccept(new UpgradeHandshake(context, upgrade).AcceptWebSocket);
                }
            }

            return _next(environment);
        }

        private class UpgradeHandshake
        {
            private IOwinContext _context;
            private OpaqueUpgrade _upgrade;
            private WebSocketFunc _webSocketFunc;

            private string _subProtocol;
            private int _receiveBufferSize = WebSocketHelpers.DefaultReceiveBufferSize;
            private TimeSpan _keepAliveInterval = WebSocket.DefaultKeepAliveInterval;
            private ArraySegment<byte> _internalBuffer;

            internal UpgradeHandshake(IOwinContext context, OpaqueUpgrade upgrade)
            {
                _context = context;
                _upgrade = upgrade;
            }

            internal void AcceptWebSocket(IDictionary<string, object> options, WebSocketFunc webSocketFunc)
            {
                _webSocketFunc = webSocketFunc;

                // Get options
                object temp;
                if (options != null && options.TryGetValue("websocket.SubProtocol", out temp))
                {
                    _subProtocol = temp as string;
                }
                if (options != null && options.TryGetValue("websocket.ReceiveBufferSize", out temp))
                {
                    _receiveBufferSize = (int)temp;
                }
                if (options != null && options.TryGetValue("websocket.KeepAliveInterval", out temp))
                {
                    _keepAliveInterval = (TimeSpan)temp;
                }
                if (options != null && options.TryGetValue("websocket.Buffer", out temp))
                {
                    _internalBuffer = (ArraySegment<byte>)temp;
                }
                else
                {
                    _internalBuffer = WebSocketBuffer.CreateInternalBufferArraySegment(_receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);
                }

                // Set WebSocket upgrade response headers

                string outgoingSecWebSocketProtocolString;
                bool shouldSendSecWebSocketProtocolHeader =
                    WebSocketHelpers.ProcessWebSocketProtocolHeader(
                        _context.Request.Headers[HttpKnownHeaderNames.SecWebSocketProtocol],
                        _subProtocol,
                        out outgoingSecWebSocketProtocolString);

                if (shouldSendSecWebSocketProtocolHeader)
                {
                    _context.Response.Headers[HttpKnownHeaderNames.SecWebSocketProtocol] = outgoingSecWebSocketProtocolString;
                }

                string secWebSocketKey = _context.Request.Headers[HttpKnownHeaderNames.SecWebSocketKey];
                string secWebSocketAccept = WebSocketHelpers.GetSecWebSocketAcceptString(secWebSocketKey);

                _context.Response.Headers[HttpKnownHeaderNames.Connection] = HttpKnownHeaderNames.Upgrade;
                _context.Response.Headers[HttpKnownHeaderNames.Upgrade] = WebSocketHelpers.WebSocketUpgradeToken;
                _context.Response.Headers[HttpKnownHeaderNames.SecWebSocketAccept] = secWebSocketAccept;

                _context.Response.StatusCode = 101; // Switching Protocols;

                _upgrade(options, OpaqueCallback);
            }

            internal async Task OpaqueCallback(IDictionary<string, object> opaqueEnv)
            {
                // Create WebSocket wrapper around the opaque env
                WebSocket webSocket = CreateWebSocket(opaqueEnv);
                OwinWebSocketWrapper wrapper = new OwinWebSocketWrapper(webSocket, (CancellationToken)opaqueEnv["opaque.CallCancelled"]);
                await _webSocketFunc(wrapper.Environment);
                // Close down the WebSocekt, gracefully if possible
                await wrapper.CleanupAsync();
            }

            private WebSocket CreateWebSocket(IDictionary<string, object> opaqueEnv)
            {
                Stream stream = (Stream)opaqueEnv["opaque.Stream"];
                return new ServerWebSocket(stream, _subProtocol, _receiveBufferSize, _keepAliveInterval, _internalBuffer);
            }
        }
    }
}
*/