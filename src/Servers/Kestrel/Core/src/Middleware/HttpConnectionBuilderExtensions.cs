// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal static class HttpConnectionBuilderExtensions
{
    public static IConnectionBuilder UseHttpServer<TContext>(this IConnectionBuilder builder, ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols, bool addAltSvcHeader) where TContext : notnull
    {
        var middleware = new HttpConnectionMiddleware<TContext>(serviceContext, application, protocols, addAltSvcHeader);
        return builder.Use(next =>
        {
            return middleware.OnConnectionAsync;
        });
    }

    public static IMultiplexedConnectionBuilder UseHttp3Server<TContext>(this IMultiplexedConnectionBuilder builder, ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols, bool addAltSvcHeader) where TContext : notnull
    {
        var middleware = new HttpMultiplexedConnectionMiddleware<TContext>(serviceContext, application, protocols, addAltSvcHeader);
        return builder.Use(next =>
        {
            return middleware.OnConnectionAsync;
        });
    }
}
