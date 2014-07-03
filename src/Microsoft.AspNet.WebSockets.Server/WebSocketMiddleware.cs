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
        private readonly RequestDelegate _next;
        private readonly WebSocketOptions _options;

        public WebSocketMiddleware(RequestDelegate next, WebSocketOptions options)
        {
            _next = next;
            _options = options;

            // TODO: validate options.
        }

        public Task Invoke(HttpContext context)
        {
            // Detect if an opaque upgrade is available. If so, add a websocket upgrade.
            var upgradeFeature = context.GetFeature<IHttpUpgradeFeature>();
            if (upgradeFeature != null)
            {
                if (_options.ReplaceFeature || context.GetFeature<IHttpWebSocketFeature>() == null)
                {
                    context.SetFeature<IHttpWebSocketFeature>(new UpgradeHandshake(context, upgradeFeature, _options));
                }
            }

            return _next(context);
        }

        private class UpgradeHandshake : IHttpWebSocketFeature
        {
            private readonly HttpContext _context;
            private readonly IHttpUpgradeFeature _upgradeFeature;
            private readonly WebSocketOptions _options;

            public UpgradeHandshake(HttpContext context, IHttpUpgradeFeature upgradeFeature, WebSocketOptions options)
            {
                _context = context;
                _upgradeFeature = upgradeFeature;
                _options = options;
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
                    throw new InvalidOperationException("Not a WebSocket request."); // TODO: LOC
                }
                
                string subProtocol = null;
                if (acceptContext != null)
                {
                    subProtocol = acceptContext.SubProtocol;
                }

                TimeSpan keepAliveInterval = _options.KeepAliveInterval;
                int receiveBufferSize = _options.ReceiveBufferSize;
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
    }
}
