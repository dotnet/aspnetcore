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
/* TODO:
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;
using Microsoft.Net.WebSockets;

namespace Microsoft.AspNet.WebSockets
{
    public class WebSocketMiddleware
    {
        private RequestDelegate _next;

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            // Detect if an opaque upgrade is available, and if websocket upgrade headers are present.
            // If so, add a websocket upgrade.
            var upgradeFeature = context.GetFeature<IHttpOpaqueUpgradeFeature>();
            if (upgradeFeature != null)
            {
                context.SetFeature<IHttpWebSocketFeature>(new UpgradeHandshake(context, upgradeFeature));
            }

            return _next(context);
        }

        private class UpgradeHandshake : IHttpWebSocketFeature
        {
            private HttpContext _context;
            private IHttpOpaqueUpgradeFeature _upgrade;

            private string _subProtocol;
            private int _receiveBufferSize = WebSocketHelpers.DefaultReceiveBufferSize;
            private TimeSpan _keepAliveInterval = WebSocket.DefaultKeepAliveInterval;
            private ArraySegment<byte>? _internalBuffer;

            internal UpgradeHandshake(HttpContext context, IHttpOpaqueUpgradeFeature upgrade)
            {
                _context = context;
                _upgrade = upgrade;
            }

            public bool IsWebSocketRequest
            {
                get
                {
                    // Headers and values:
                    // Connection: Upgrade
                    // Upgrade: WebSocket
                    // Sec-WebSocket-Version: (WebSocketProtocolComponent.SupportedVersion)
                    // Sec-WebSocket-Key: (hash, see WebSocketHelpers.GetSecWebSocketAcceptString)
                    // Sec-WebSocket-Protocol: (optional, list)
                    IList<string> connectionHeaders = _context.Request.Headers.GetCommaSeparatedValues(HttpKnownHeaderNames.Connection); // "Upgrade, KeepAlive"
                    string upgradeHeader = _context.Request.Headers[HttpKnownHeaderNames.Upgrade];
                    string versionHeader = _context.Request.Headers[HttpKnownHeaderNames.SecWebSocketVersion];
                    string keyHeader = _context.Request.Headers[HttpKnownHeaderNames.SecWebSocketKey];

                    if (connectionHeaders != null && connectionHeaders.Count > 0
                        && connectionHeaders.Contains(HttpKnownHeaderNames.Upgrade, StringComparer.OrdinalIgnoreCase)
                        && string.Equals(upgradeHeader, WebSocketHelpers.WebSocketUpgradeToken, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(versionHeader, UnsafeNativeMethods.WebSocketProtocolComponent.SupportedVersion, StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(keyHeader))
                    {
                        return true;
                    }
                    return false;
                }
            }

            public async Task<WebSocket> AcceptAsync(IWebSocketAcceptContext acceptContext)
            {
                // Get options
                if (acceptContext != null)
                {
                    _subProtocol = acceptContext.SubProtocol;
                }

                var advancedAcceptContext = acceptContext as WebSocketAcceptContext;
                if (advancedAcceptContext != null)
                {
                    if (advancedAcceptContext.ReceiveBufferSize.HasValue)
                    {
                        _receiveBufferSize = advancedAcceptContext.ReceiveBufferSize.Value;
                    }
                    if (advancedAcceptContext.KeepAliveInterval.HasValue)
                    {
                        _keepAliveInterval = advancedAcceptContext.KeepAliveInterval.Value;
                    }
                    _internalBuffer = advancedAcceptContext.Buffer;
                }

                if (!_internalBuffer.HasValue)
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

                // 101 Switching Protocols;
                Stream opaqueTransport = await _upgrade.UpgradeAsync();
                return new ServerWebSocket(opaqueTransport, _subProtocol, _receiveBufferSize, _keepAliveInterval, _internalBuffer.Value);
            }
        }

        public class WebSocketAcceptContext : IWebSocketAcceptContext
        {
            public string SubProtocol { get; set; }
            public int? ReceiveBufferSize { get; set; }
            public TimeSpan? KeepAliveInterval { get; set; }
            public ArraySegment<byte>? Buffer { get; set; }
        }
    }
}*/