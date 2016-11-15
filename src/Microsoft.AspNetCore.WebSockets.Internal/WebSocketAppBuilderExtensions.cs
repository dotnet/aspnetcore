// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Channels;
using Microsoft.AspNetCore.WebSockets.Internal;

namespace Microsoft.AspNetCore.Builder
{
    public static class WebSocketAppBuilderExtensions
    {
        public static void UseWebSocketConnections(this IApplicationBuilder self)
        {
            // Only the GC can clean up this channel factory :(
            self.UseWebSocketConnections(new ChannelFactory(), new WebSocketConnectionOptions());
        }

        public static void UseWebSocketConnections(this IApplicationBuilder self, ChannelFactory channelFactory)
        {
            if (channelFactory == null)
            {
                throw new ArgumentNullException(nameof(channelFactory));
            }
            self.UseWebSocketConnections(channelFactory, new WebSocketConnectionOptions());
        }

        public static void UseWebSocketConnections(this IApplicationBuilder self, ChannelFactory channelFactory, WebSocketConnectionOptions options)
        {
            if (channelFactory == null)
            {
                throw new ArgumentNullException(nameof(channelFactory));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            self.UseMiddleware<WebSocketConnectionMiddleware>(channelFactory, options);
        }
    }
}
