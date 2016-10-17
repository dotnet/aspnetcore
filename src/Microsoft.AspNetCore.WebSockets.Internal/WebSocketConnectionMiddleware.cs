// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebSockets.Internal
{
    public class WebSocketConnectionMiddleware
    {
        private readonly ChannelFactory _channelFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly RequestDelegate _next;
        private readonly WebSocketConnectionOptions _options;

        public WebSocketConnectionMiddleware(RequestDelegate next, ChannelFactory channelFactory, WebSocketConnectionOptions options, ILoggerFactory loggerFactory)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (channelFactory == null)
            {
                throw new ArgumentNullException(nameof(channelFactory));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _next = next;
            _loggerFactory = loggerFactory;
            _channelFactory = channelFactory;
            _options = options;
        }

        public Task Invoke(HttpContext context)
        {
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
            if (upgradeFeature != null)
            {
                if (_options.ReplaceFeature || context.Features.Get<IHttpWebSocketConnectionFeature>() == null)
                {
                    context.Features.Set<IHttpWebSocketConnectionFeature>(new WebSocketConnectionFeature(context, _channelFactory, upgradeFeature, _loggerFactory));
                }
            }

            return _next(context);
        }
    }
}
