// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic
{
    /// <summary>
    /// Options for Quic based connections.
    /// </summary>
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
        public string? Alpn { get; set; }

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

        internal ISystemClock SystemClock = new SystemClock();
    }
}
