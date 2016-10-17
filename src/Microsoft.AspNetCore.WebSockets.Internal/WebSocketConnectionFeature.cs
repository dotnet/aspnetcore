// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebSockets.Internal;

namespace Microsoft.AspNetCore.WebSockets.Internal
{
    internal class WebSocketConnectionFeature : IHttpWebSocketConnectionFeature
    {
        private HttpContext _context;
        private IHttpUpgradeFeature _upgradeFeature;
        private ILogger _logger;
        private readonly ChannelFactory _channelFactory;

        public bool IsWebSocketRequest
        {
            get
            {
                if (!_upgradeFeature.IsUpgradableRequest)
                {
                    return false;
                }
                return HandshakeHelpers.CheckSupportedWebSocketRequest(_context.Request);
            }
        }

        public WebSocketConnectionFeature(HttpContext context, ChannelFactory channelFactory, IHttpUpgradeFeature upgradeFeature, ILoggerFactory loggerFactory)
        {
            _channelFactory = channelFactory;
            _context = context;
            _upgradeFeature = upgradeFeature;
            _logger = loggerFactory.CreateLogger<WebSocketConnectionFeature>();
        }

        public ValueTask<IWebSocketConnection> AcceptWebSocketConnectionAsync(WebSocketAcceptContext acceptContext)
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

            _logger.LogDebug("WebSocket Handshake completed. SubProtocol: {0}", subProtocol);

            var key = string.Join(", ", _context.Request.Headers[Constants.Headers.SecWebSocketKey]);

            var responseHeaders = HandshakeHelpers.GenerateResponseHeaders(key, subProtocol);
            foreach (var headerPair in responseHeaders)
            {
                _context.Response.Headers[headerPair.Key] = headerPair.Value;
            }

            // TODO: Avoid task allocation if there's a ValueTask-based UpgradeAsync?
            return new ValueTask<IWebSocketConnection>(AcceptWebSocketConnectionCoreAsync(subProtocol));
        }

        private async Task<IWebSocketConnection> AcceptWebSocketConnectionCoreAsync(string subProtocol)
        {
            _logger.LogDebug("Upgrading connection to WebSockets");
            var opaqueTransport = await _upgradeFeature.UpgradeAsync();
            var connection = new WebSocketConnection(
                opaqueTransport.AsReadableChannel(),
                _channelFactory.MakeWriteableChannel(opaqueTransport),
                subProtocol: subProtocol);
            return connection;
        }
    }
}