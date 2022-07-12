// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http3Cat;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace Microsoft.Extensions.DependencyInjection;

internal static class Http3CatIServiceCollectionExtensions
{
    public static IServiceCollection UseHttp3Cat(this IServiceCollection services, Action<Http3CatOptions> configureOptions)
    {
        services.AddSingleton<IConnectionFactory, SocketConnectionFactory>();
        services.AddHostedService<Http3CatHostedService>();
        services.Configure(configureOptions);
        return services;
    }
}
