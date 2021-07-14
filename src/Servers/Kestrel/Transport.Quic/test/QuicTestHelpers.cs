// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests
{
    internal static class QuicTestHelpers
    {
        public const string Alpn = "h3-29";
        private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

        public static QuicTransportFactory CreateTransportFactory(ILoggerFactory loggerFactory = null, ISystemClock systemClock = null)
        {
            var quicTransportOptions = new QuicTransportOptions();
            quicTransportOptions.Alpn = Alpn;
            quicTransportOptions.IdleTimeout = TimeSpan.FromMinutes(1);
            quicTransportOptions.MaxBidirectionalStreamCount = 200;
            quicTransportOptions.MaxUnidirectionalStreamCount = 200;
            if (systemClock != null)
            {
                quicTransportOptions.SystemClock = systemClock;
            }

            return new QuicTransportFactory(loggerFactory ?? NullLoggerFactory.Instance, Options.Create(quicTransportOptions));
        }

        public static async Task<QuicConnectionListener> CreateConnectionListenerFactory(ILoggerFactory loggerFactory = null, ISystemClock systemClock = null)
        {
            var transportFactory = CreateTransportFactory(loggerFactory, systemClock);

            // Use ephemeral port 0. OS will assign unused port.
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);

            var features = CreateBindAsyncFeatures();
            return (QuicConnectionListener)await transportFactory.BindAsync(endpoint, features, cancellationToken: CancellationToken.None);
        }

        public static FeatureCollection CreateBindAsyncFeatures()
        {
            var cert = TestResources.GetTestCertificate();

            var sslServerAuthenticationOptions = new SslServerAuthenticationOptions();
            sslServerAuthenticationOptions.ServerCertificate = cert;
            sslServerAuthenticationOptions.RemoteCertificateValidationCallback = RemoteCertificateValidationCallback;

            var features = new FeatureCollection();
            features.Set(sslServerAuthenticationOptions);

            return features;
        }

        public static async ValueTask<MultiplexedConnectionContext> AcceptAndAddFeatureAsync(this IMultiplexedConnectionListener listener)
        {
            var connection = await listener.AcceptAsync();
            connection.Features.Set<IConnectionHeartbeatFeature>(new TestConnectionHeartbeatFeature());
            return connection;
        }

        private class TestConnectionHeartbeatFeature : IConnectionHeartbeatFeature
        {
            public void OnHeartbeat(Action<object> action, object state)
            {
            }
        }

        private static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static QuicClientConnectionOptions CreateClientConnectionOptions(EndPoint remoteEndPoint)
        {
            return new QuicClientConnectionOptions
            {
                MaxBidirectionalStreams = 200,
                MaxUnidirectionalStreams = 200,
                RemoteEndPoint = remoteEndPoint,
                ClientAuthenticationOptions = new SslClientAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol>
                    {
                        new SslApplicationProtocol(Alpn)
                    },
                    RemoteCertificateValidationCallback = RemoteCertificateValidationCallback
                }
            };
        }

        public static async Task<QuicStreamContext> CreateAndCompleteBidirectionalStreamGracefully(QuicConnection clientConnection, MultiplexedConnectionContext serverConnection)
        {
            var clientStream = clientConnection.OpenBidirectionalStream();
            await clientStream.WriteAsync(TestData, endStream: true).DefaultTimeout();
            var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
            var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
            serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

            // Input should be completed.
            readResult = await serverStream.Transport.Input.ReadAsync();
            Assert.True(readResult.IsCompleted);

            // Complete reading and writing.
            await serverStream.Transport.Input.CompleteAsync();
            await serverStream.Transport.Output.CompleteAsync();

            var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

            // Both send and receive loops have exited.
            await quicStreamContext._processingTask.DefaultTimeout();
            Assert.True(quicStreamContext.CanWrite);
            Assert.True(quicStreamContext.CanRead);

            await quicStreamContext.DisposeAsync();

            return quicStreamContext;
        }
    }
}
