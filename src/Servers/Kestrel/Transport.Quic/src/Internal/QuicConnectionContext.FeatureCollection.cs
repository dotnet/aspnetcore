// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal sealed partial class QuicConnectionContext : IProtocolErrorCodeFeature, ITlsConnectionFeature
    {
        public long Error { get; set; }

        // Support accessing client certificate
        // https://github.com/dotnet/aspnetcore/issues/34756
        public X509Certificate2? ClientCertificate
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private void InitializeFeatures()
        {
            _currentIProtocolErrorCodeFeature = this;
            _currentITlsConnectionFeature = this;
        }
    }
}
