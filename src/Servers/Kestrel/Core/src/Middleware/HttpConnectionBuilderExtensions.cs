// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal static class HttpConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseHttpServer<TContext>(this IConnectionBuilder builder, ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols)
        {
            var middleware = new HttpConnectionMiddleware<TContext>(serviceContext, application, protocols);
            return builder.Use(next =>
            {
                return middleware.OnConnectionAsync;
            });
        }

        public static IMultiplexedConnectionBuilder UseHttp3Server<TContext>(this IMultiplexedConnectionBuilder builder, ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols)
        {
            var middleware = new Http3ConnectionMiddleware<TContext>(serviceContext, application);
            return builder.Use(next =>
            {
                return middleware.OnConnectionAsync;
            });
        }
    }
}
