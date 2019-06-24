using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ClientSample;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class TcpHubConnectionBuilderExtensions
    {
        private static readonly Uri _ignoredEndpoint = new Uri("https://www.example.com");

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

            // Set HttpConnectionOptions.Url, so HubConnectionBuilder.Build() doesn't complain about no URL being configured.
            builder.Services.Configure<HttpConnectionOptions>(o =>
            {
                o.Url = _ignoredEndpoint;
            });

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
                // HubConnection should be passing in the HttpEndPoint configured by WithEndPoint. Just ignore it.
                Trace.Assert(ReferenceEquals(((HttpEndPoint)endPoint).Url, _ignoredEndpoint));

                return new TcpConnection(_endPoint).StartAsync();
            }
        }
    }
}
