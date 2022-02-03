// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ClientSample;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Client;

public static class TcpHubConnectionBuilderExtensions
{
    public static IHubConnectionBuilder WithEndPoint(this IHubConnectionBuilder builder, Uri uri)
    {
        if (!string.Equals(uri.Scheme, "net.tcp", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"URI Scheme {uri.Scheme} not supported.");
        }

        IPEndPoint endPoint;
        if (string.Equals(uri.Host, "localhost"))
        {
            endPoint = new IPEndPoint(IPAddress.Loopback, uri.Port);
        }
        else
        {
            endPoint = new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port);
        }

        return builder.WithEndPoint(endPoint);
    }

    public static IHubConnectionBuilder WithEndPoint(this IHubConnectionBuilder builder, EndPoint endPoint)
    {
        builder.Services.AddSingleton<IConnectionFactory, TcpConnectionFactory>();
        builder.Services.AddSingleton(endPoint);

        return builder;
    }

    private class TcpConnectionFactory : IConnectionFactory
    {
        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            return new TcpConnection(endPoint).StartAsync();
        }
    }
}
