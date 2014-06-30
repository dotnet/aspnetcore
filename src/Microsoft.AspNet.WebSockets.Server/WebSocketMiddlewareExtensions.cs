// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.WebSockets.Server;

namespace Microsoft.AspNet.Builder
{
    public static class WebSocketMiddlewareExtensions
    {
        public static IBuilder UseWebSockets(this IBuilder builder)
        {
            return builder.UseWebSockets(new WebSocketOptions());
        }

        public static IBuilder UseWebSockets(this IBuilder builder, WebSocketOptions options) // TODO: [NotNull]
        {
            return builder.Use(next => new WebSocketMiddleware(next, options).Invoke);
        }
    }
}