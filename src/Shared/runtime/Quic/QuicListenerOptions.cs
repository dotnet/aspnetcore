// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Net.Security;

namespace System.Net.Quic
{
    /// <summary>
    /// Options to provide to the <see cref="QuicListener"/>.
    /// </summary>
    internal class QuicListenerOptions
    {
        /// <summary>
        /// Server Ssl options to use for ALPN, SNI, etc.
        /// </summary>
        public SslServerAuthenticationOptions? ServerAuthenticationOptions { get; set; }

        /// <summary>
        /// Optional path to certificate file to configure the security configuration.
        /// </summary>
        public string? CertificateFilePath { get; set; }

        /// <summary>
        /// Optional path to private key file to configure the security configuration.
        /// </summary>
        public string? PrivateKeyFilePath { get; set; }

        /// <summary>
        /// The endpoint to listen on.
        /// </summary>
        public IPEndPoint? ListenEndPoint { get; set; }

        /// <summary>
        /// Number of connections to be held without accepting the connection.
        /// </summary>
        public int ListenBacklog { get; set; } = 512;

        /// <summary>
        /// Limit on the number of bidirectional streams an accepted connection can create
        /// back to the client.
        /// Default is 100.
        /// </summary>
        // TODO consider constraining these limits to 0 to whatever the max of the QUIC library we are using.
        public long MaxBidirectionalStreams { get; set; } = 100;

        /// <summary>
        /// Limit on the number of unidirectional streams the peer connection can create.
        /// Default is 100.
        /// </summary>
        // TODO consider constraining these limits to 0 to whatever the max of the QUIC library we are using.
        public long MaxUnidirectionalStreams { get; set; } = 100;

        /// <summary>
        /// Idle timeout for connections, afterwhich the connection will be closed.
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(10);
    }
}
