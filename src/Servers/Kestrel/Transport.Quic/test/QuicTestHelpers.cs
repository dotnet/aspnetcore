// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests
{
    internal static class QuicTestHelpers
    {
        public static QuicTransportFactory CreateTransportFactory()
        {
            var quicTransportOptions = new QuicTransportOptions();
            quicTransportOptions.Alpn = "h3-29";
            quicTransportOptions.IdleTimeout = TimeSpan.FromMinutes(1);

            return new QuicTransportFactory(NullLoggerFactory.Instance, Options.Create(quicTransportOptions));
        }

        public static async Task<QuicConnectionListener> CreateConnectionListenerFactory()
        {
            var transportFactory = CreateTransportFactory();

            var features = CreateBindAsyncFeatures();
            return (QuicConnectionListener)await transportFactory.BindAsync(new IPEndPoint(IPAddress.Loopback, 5001), features, cancellationToken: CancellationToken.None);
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

        private static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static QuicClientConnectionOptions CreateClientConnectionOptions(EndPoint remoteEndPoint)
        {
            return new QuicClientConnectionOptions
            {
                MaxBidirectionalStreams = 10,
                MaxUnidirectionalStreams = 20,
                RemoteEndPoint = remoteEndPoint,
                ClientAuthenticationOptions = new SslClientAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol>
                    {
                        new SslApplicationProtocol("h3-29")
                    },
                    RemoteCertificateValidationCallback = RemoteCertificateValidationCallback
                }
            };
        }
    }
}
