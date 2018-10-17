// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebSockets.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.WebSockets
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketOptions _options;

        public WebSocketMiddleware(RequestDelegate next, IOptions<WebSocketOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options.Value;

            // TODO: validate options.
        }

        public Task Invoke(HttpContext context)
        {
            // Detect if an opaque upgrade is available. If so, add a websocket upgrade.
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
            if (upgradeFeature != null && context.Features.Get<IHttpWebSocketFeature>() == null)
            {
                context.Features.Set<IHttpWebSocketFeature>(new UpgradeHandshake(context, upgradeFeature, _options));
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

            public async Task<WebSocket> AcceptAsync(WebSocketAcceptContext acceptContext)
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
                var advancedAcceptContext = acceptContext as ExtendedWebSocketAcceptContext;
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

                return WebSocketProtocol.CreateFromStream(opaqueTransport, isServer: true, subProtocol: subProtocol, keepAliveInterval: keepAliveInterval);
            }
        }
    }
}
