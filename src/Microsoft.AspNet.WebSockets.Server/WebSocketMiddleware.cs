// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.WebSockets.Protocol;

namespace Microsoft.AspNet.WebSockets.Server
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
            private IHttpOpaqueUpgradeFeature _upgradeFeature;

            public UpgradeHandshake(HttpContext context, IHttpOpaqueUpgradeFeature upgradeFeature)
            {
                _context = context;
                _upgradeFeature = upgradeFeature;
            }

            public bool IsWebSocketRequest
            {
                get
                {
                    if (!_upgradeFeature.IsUpgradableRequest)
                    {
                        return false;
                    }
                    var headers = new List<KeyValuePair<string, string>>();
                    foreach (string headerName in HandshakeHelpers.NeededHeaders)
                    {
                        foreach (var value in _context.Request.Headers.GetCommaSeparatedValues(headerName))
                        {
                            headers.Add(new KeyValuePair<string, string>(headerName, value));
                        }
                    }
                    return HandshakeHelpers.CheckSupportedWebSocketRequest(_context.Request.Method, headers);
                }
            }

            public async Task<WebSocket> AcceptAsync(IWebSocketAcceptContext acceptContext)
            {
                if (!IsWebSocketRequest)
                {
                    throw new InvalidOperationException("Not a WebSocket request.");
                }
                
                string subProtocol = null;
                if (acceptContext != null)
                {
                    subProtocol = acceptContext.SubProtocol;
                }

                TimeSpan keepAliveInterval = TimeSpan.FromMinutes(2); // TODO:
                int receiveBufferSize = 4 * 1024; // TODO:
                var advancedAcceptContext = acceptContext as WebSocketAcceptContext;
                if (advancedAcceptContext != null)
                {
                    if (advancedAcceptContext.ReceiveBufferSize.HasValue)
                    {
                        receiveBufferSize = advancedAcceptContext.ReceiveBufferSize.Value;
                    }
                    if (advancedAcceptContext.KeepAliveInterval.HasValue)
                    {
                        keepAliveInterval = advancedAcceptContext.KeepAliveInterval.Value;
                    }
                }

                string key = string.Join(", ", _context.Request.Headers[Constants.Headers.SecWebSocketKey]);

                var responseHeaders = HandshakeHelpers.GenerateResponseHeaders(key, subProtocol);
                foreach (var headerPair in responseHeaders)
                {
                    _context.Response.Headers[headerPair.Key] = headerPair.Value;
                }
                Stream opaqueTransport = await _upgradeFeature.UpgradeAsync(); // Sets status code to 101

                return CommonWebSocket.CreateServerWebSocket(opaqueTransport, subProtocol, keepAliveInterval, receiveBufferSize);
            }
        }

        public class WebSocketAcceptContext : IWebSocketAcceptContext
        {
            public string SubProtocol { get; set; }
            public int? ReceiveBufferSize { get; set; }
            public TimeSpan? KeepAliveInterval { get; set; }
            // public ArraySegment<byte>? Buffer { get; set; } // TODO
        }
    }
}
