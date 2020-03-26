// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic
{
    public class QuicTransportOptions
    {
        /// <summary>
        /// The maximum number of concurrent bi-directional streams per connection.
        /// </summary>
        public ushort MaxBidirectionalStreamCount { get; set; } = 100;

        /// <summary>
        /// The maximum number of concurrent inbound uni-directional streams per connection.
        /// </summary>
        public ushort MaxUnidirectionalStreamCount { get; set; } = 10;

        /// <summary>
        /// The Application Layer Protocol Negotiation string.
        /// </summary>
        public string Alpn { get; set; }

        /// <summary>
        /// The certificate that MsQuic will use.
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// Optional path to certificate file to configure the security configuration.
        /// </summary>
        public string CertificateFilePath { get; set; }

        /// <summary>
        /// Optional path to private key file to configure the security configuration.
        /// </summary>
        public string PrivateKeyFilePath { get; set; }

        /// <summary>
        /// Sets the idle timeout for connections and streams.
        /// </summary>
        public TimeSpan IdleTimeout { get; set; }

        /// <summary>
        /// The maximum read size.
        /// </summary>
        public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

        /// <summary>
        /// The maximum write size.
        /// </summary>
        public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

        internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = System.Buffers.SlabMemoryPoolFactory.Create;

    }
}
