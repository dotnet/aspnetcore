// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebSockets
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketOptions _options;
        private readonly ILogger _logger;
        private readonly bool _anyOriginAllowed;
        private readonly List<string> _allowedOrigins;

        public WebSocketMiddleware(RequestDelegate next, IOptions<WebSocketOptions> options, ILoggerFactory loggerFactory)
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
            _allowedOrigins = _options.AllowedOrigins.Select(o => o.ToLowerInvariant()).ToList();
            _anyOriginAllowed = _options.AllowedOrigins.Count == 0 || _options.AllowedOrigins.Contains("*", StringComparer.Ordinal);

            _logger = loggerFactory.CreateLogger<WebSocketMiddleware>();

            // TODO: validate options.
        }

        public Task Invoke(HttpContext context)
        {
            // Detect if an opaque upgrade is available. If so, add a websocket upgrade.
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
            if (upgradeFeature != null && context.Features.Get<IHttpWebSocketFeature>() == null)
            {
                var webSocketFeature = new UpgradeHandshake(context, upgradeFeature, _options);
                context.Features.Set<IHttpWebSocketFeature>(webSocketFeature);

                if (!_anyOriginAllowed)
                {
                    // Check for Origin header
                    var originHeader = context.Request.Headers[HeaderNames.Origin];

                    if (!StringValues.IsNullOrEmpty(originHeader) && webSocketFeature.IsWebSocketRequest)
                    {
                        // Check allowed origins to see if request is allowed
                        if (!_allowedOrigins.Contains(originHeader.ToString(), StringComparer.Ordinal))
                        {
                            _logger.LogDebug("Request origin {Origin} is not in the list of allowed origins.", originHeader);
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }
                    }
                }
            }

            return _next(context);
        }

        private class UpgradeHandshake : IHttpWebSocketFeature
        {
            private readonly HttpContext _context;
            private readonly IHttpUpgradeFeature _upgradeFeature;
            private readonly WebSocketOptions _options;
            private bool? _isWebSocketRequest;

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
                    if (_isWebSocketRequest == null)
                    {
                        if (!_upgradeFeature.IsUpgradableRequest)
                        {
                            _isWebSocketRequest = false;
                        }
                        else
                        {
                            var headers = new List<KeyValuePair<string, string>>();
                            foreach (string headerName in HandshakeHelpers.NeededHeaders)
                            {
                                foreach (var value in _context.Request.Headers.GetCommaSeparatedValues(headerName))
                                {
                                    headers.Add(new KeyValuePair<string, string>(headerName, value));
                                }
                            }
                            _isWebSocketRequest = HandshakeHelpers.CheckSupportedWebSocketRequest(_context.Request.Method, headers);
                        }
                    }
                    return _isWebSocketRequest.Value;
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

                string key = _context.Request.Headers[HeaderNames.SecWebSocketKey];

                HandshakeHelpers.GenerateResponseHeaders(key, subProtocol, _context.Response.Headers);

                Stream opaqueTransport = await _upgradeFeature.UpgradeAsync(); // Sets status code to 101

                return WebSocket.CreateFromStream(opaqueTransport, isServer: true, subProtocol: subProtocol, keepAliveInterval: keepAliveInterval);
            }
        }
    }
}
