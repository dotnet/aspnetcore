// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal sealed partial class QuicConnectionContext : IProtocolErrorCodeFeature, ITlsConnectionFeature, ITlsApplicationProtocolFeature
    {

        private X509Certificate2? _clientCert;

        public long Error { get; set; }

        // Support accessing client certificate
        // https://github.com/dotnet/aspnetcore/issues/34756
        public X509Certificate2? ClientCertificate
        {
            get { return _clientCert ??= (X509Certificate2?)_connection.RemoteCertificate; }
            set { _clientCert = !(_clientCert is null) ? value : _clientCert; }
        }

        public ReadOnlyMemory<byte> ApplicationProtocol => _connection.NegotiatedApplicationProtocol.Protocol;

        public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((X509Certificate2?)_connection.RemoteCertificate);
        }

        private void InitializeFeatures()
        {
            _currentIProtocolErrorCodeFeature = this;
            _currentITlsConnectionFeature = this;
            _currentITlsApplicationProtocolFeature = this;
        }
    }
}
