// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Net.Security;

namespace System.Net.Quic
{
    /// <summary>
    /// Options to provide to the <see cref="QuicConnection"/> when connecting to a Listener.
    /// </summary>
    internal class QuicClientConnectionOptions
    {
        /// <summary>
        /// Client authentication options to use when establishing a <see cref="QuicConnection"/>.
        /// </summary>
        public SslClientAuthenticationOptions? ClientAuthenticationOptions { get; set; }

        /// <summary>
        /// The local endpoint that will be bound to.
        /// </summary>
        public IPEndPoint? LocalEndPoint { get; set; }

        /// <summary>
        /// The endpoint to connect to.
        /// </summary>
        public IPEndPoint? RemoteEndPoint { get; set; }

        /// <summary>
        /// Limit on the number of bidirectional streams the peer connection can create
        /// on an accepted connection.
        /// Default is 100.
        /// </summary>
        // TODO consider constraining these limits to 0 to whatever the max of the QUIC library we are using.
        public long MaxBidirectionalStreams { get; set; } = 100;

        /// <summary>
        /// Limit on the number of unidirectional streams the peer connection can create
        /// on an accepted connection.
        /// Default is 100.
        /// </summary>
        // TODO consider constraining these limits to 0 to whatever the max of the QUIC library we are using.
        public long MaxUnidirectionalStreams { get; set; } = 100;

        /// <summary>
        /// Idle timeout for connections, afterwhich the connection will be closed.
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(2);
    }
}
