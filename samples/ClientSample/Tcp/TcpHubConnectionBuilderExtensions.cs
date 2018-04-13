using System;
using System.Net;
using ClientSample;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.SignalR.Client
{
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
            builder.WithConnectionFactory(
                format => new TcpConnection(endPoint).StartAsync(),
                connection => ((TcpConnection)connection).DisposeAsync()
            );

            return builder;
        }
    }
}
