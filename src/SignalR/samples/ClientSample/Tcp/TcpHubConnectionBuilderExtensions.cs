using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ClientSample;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;
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
            builder.Services.AddSingleton<IConnectionFactory>(new TcpConnectionFactory(endPoint));

            return builder;
        }

        private class TcpConnectionFactory : IConnectionFactory
        {
            private readonly EndPoint _endPoint;

            public TcpConnectionFactory(EndPoint endPoint)
            {
                _endPoint = endPoint;
            }

            public ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
            {
                // HubConnection should be passing in a default HttpEndPoint based on the unconfigured HttpConnectionOptions. Just ignore it.
                Trace.Assert(endPoint is HttpEndPoint);
                Trace.Assert(((HttpEndPoint)endPoint).Url == null);

                return new TcpConnection(_endPoint).StartAsync();
            }

            public Task DisposeAsync(ConnectionContext connection)
            {
                return connection.DisposeAsync().AsTask();
            }
        }
    }
}
