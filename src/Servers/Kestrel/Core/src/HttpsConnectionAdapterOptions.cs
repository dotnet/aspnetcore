// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Microsoft.AspNetCore.Server.Kestrel.Https
{
    /// <summary>
    /// Settings for how Kestrel should handle HTTPS connections.
    /// </summary>
    public class HttpsConnectionAdapterOptions
    {
        private TimeSpan _handshakeTimeout;

        /// <summary>
        /// Initializes a new instance of <see cref="HttpsConnectionAdapterOptions"/>.
        /// </summary>
        public HttpsConnectionAdapterOptions()
        {
            ClientCertificateMode = ClientCertificateMode.NoCertificate;
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11;
            HandshakeTimeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// <para>
        /// Specifies the server certificate used to authenticate HTTPS connections. This is ignored if ServerCertificateSelector is set.
        /// </para>
        /// <para>
        /// If the server certificate has an Extended Key Usage extension, the usages must include Server Authentication (OID 1.3.6.1.5.5.7.3.1).
        /// </para>
        /// </summary>
        public X509Certificate2 ServerCertificate { get; set; }

        /// <summary>
        /// <para>
        /// A callback that will be invoked to dynamically select a server certificate. This is higher priority than ServerCertificate.
        /// If SNI is not available then the name parameter will be null.
        /// </para>
        /// <para>
        /// If the server certificate has an Extended Key Usage extension, the usages must include Server Authentication (OID 1.3.6.1.5.5.7.3.1).
        /// </para>
        /// </summary>
        public Func<ConnectionContext, string, X509Certificate2> ServerCertificateSelector { get; set; }

        /// <summary>
        /// Specifies the client certificate requirements for a HTTPS connection. Defaults to <see cref="ClientCertificateMode.NoCertificate"/>.
        /// </summary>
        public ClientCertificateMode ClientCertificateMode { get; set; }

        /// <summary>
        /// Specifies a callback for additional client certificate validation that will be invoked during authentication.
        /// </summary>
        public Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> ClientCertificateValidation { get; set; }

        /// <summary>
        /// Specifies allowable SSL protocols. Defaults to <see cref="SslProtocols.Tls12" /> and <see cref="SslProtocols.Tls11"/>.
        /// </summary>
        public SslProtocols SslProtocols { get; set; }

        /// <summary>
        /// The protocols enabled on this endpoint.
        /// </summary>
        /// <remarks>Defaults to HTTP/1.x only.</remarks>
        internal HttpProtocols HttpProtocols { get; set; }

        /// <summary>
        /// Specifies whether the certificate revocation list is checked during authentication.
        /// </summary>
        public bool CheckCertificateRevocation { get; set; }

        /// <summary>
        /// Specifies the maximum amount of time allowed for the TLS/SSL handshake. This must be positive and finite.
        /// </summary>
        public TimeSpan HandshakeTimeout
        {
            get => _handshakeTimeout;
            set
            {
                if (value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.PositiveTimeSpanRequired);
                }
                _handshakeTimeout = value != Timeout.InfiniteTimeSpan ? value : TimeSpan.MaxValue;
            }
        }

        // For testing
        internal Action OnHandshakeStarted;
    }
}
