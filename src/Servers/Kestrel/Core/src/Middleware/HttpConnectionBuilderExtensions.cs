// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal static class HttpConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseConnectionLimit(this IConnectionBuilder builder, KestrelServerOptions options, IKestrelTrace trace)
        {
            // Add the connection limit middleware
            if (options.Limits.MaxConcurrentConnections.HasValue)
            {
                return builder.Use(next => new ConnectionLimitMiddleware(next, options.Limits.MaxConcurrentConnections.Value, trace).OnConnectionAsync);
            }
            return builder;
        }

        public static IConnectionBuilder UseHttpServer<TContext>(this IConnectionBuilder builder, ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols)
        {
            var middleware = new HttpConnectionMiddleware<TContext>(serviceContext, application, protocols);
            return builder.Use(next =>
            {
                return middleware.OnConnectionAsync;
            });
        }
    }
}
