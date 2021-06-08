// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class TlsConnectionFeature : ITlsConnectionFeature, ITlsApplicationProtocolFeature, ITlsHandshakeFeature
    {
        private readonly SslStream _sslStream;
        private X509Certificate2? _clientCert;
        private ReadOnlyMemory<byte>? _applicationProtocol;
        private SslProtocols? _protocol;
        private CipherAlgorithmType? _cipherAlgorithm;
        private int? _cipherStrength;
        private HashAlgorithmType? _hashAlgorithm;
        private int? _hashStrength;
        private ExchangeAlgorithmType? _keyExchangeAlgorithm;
        private int? _keyExchangeStrength;

        public TlsConnectionFeature(SslStream sslStream)
        {
            if (sslStream is null)
            {
                throw new ArgumentNullException(nameof(sslStream));
            }

            _sslStream = sslStream;
        }

        public X509Certificate2? ClientCertificate
        {
            get
            {
                return _clientCert ??= ConvertToX509Certificate2(_sslStream.RemoteCertificate);
            }
            set => _clientCert = value;
        }

        // Used for event source, not part of any of the feature interfaces.
        public string? HostName { get; set; }

        public ReadOnlyMemory<byte> ApplicationProtocol
        {
            get => _applicationProtocol ?? _sslStream.NegotiatedApplicationProtocol.Protocol;
            set => _applicationProtocol = value;
        }

        public SslProtocols Protocol
        {
            get => _protocol ?? _sslStream.SslProtocol;
            set => _protocol = value;
        }

        // We don't store the values for these because they could be changed by a renegotiation.
        public CipherAlgorithmType CipherAlgorithm
        {
            get => _cipherAlgorithm ?? _sslStream.CipherAlgorithm;
            set => _cipherAlgorithm = value;
        }

        public int CipherStrength
        {
            get => _cipherStrength ?? _sslStream.CipherStrength;
            set => _cipherStrength = value;
        }

        public HashAlgorithmType HashAlgorithm
        {
            get => _hashAlgorithm ?? _sslStream.HashAlgorithm;
            set => _hashAlgorithm = value;
        }

        public int HashStrength
        {
            get => _hashStrength ?? _sslStream.HashStrength;
            set => _hashStrength = value;
        }

        public ExchangeAlgorithmType KeyExchangeAlgorithm
        {
            get => _keyExchangeAlgorithm ?? _sslStream.KeyExchangeAlgorithm;
            set => _keyExchangeAlgorithm = value;
        }

        public int KeyExchangeStrength
        {
            get => _keyExchangeStrength ?? _sslStream.KeyExchangeStrength;
            set => _keyExchangeStrength = value;
        }

        public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ClientCertificate);
        }

        private static X509Certificate2? ConvertToX509Certificate2(X509Certificate? certificate)
        {
            return certificate switch
            {
                null => null,
                X509Certificate2 cert2 => cert2,
                _ => new X509Certificate2(certificate),
            };
        }
    }
}
