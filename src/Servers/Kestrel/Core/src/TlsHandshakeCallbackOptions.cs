// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Microsoft.AspNetCore.Server.Kestrel.Https
{
    /// <summary>
    /// Options used to configure a per connection callback for TLS configuration.
    /// </summary>
    public class TlsHandshakeCallbackOptions
    {
        private TimeSpan _handshakeTimeout = HttpsConnectionAdapterOptions.DefaultHandshakeTimeout;

        /// <summary>
        /// The callback to invoke per connection. This property is required.
        /// </summary>
        public Func<TlsHandshakeCallbackContext, ValueTask<SslServerAuthenticationOptions>> OnConnection { get; set; } = default!;

        /// <summary>
        /// Optional application state to flow to the <see cref="OnConnection"/> callback.
        /// </summary>
        public object? OnConnectionState { get; set; }

        /// <summary>
        /// Specifies the maximum amount of time allowed for the TLS/SSL handshake. This must be positive
        /// or <see cref="Timeout.InfiniteTimeSpan"/>. Defaults to 10 seconds.
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

        // Copied from the ListenOptions to enable ALPN
        internal HttpProtocols HttpProtocols { get; set; }
    }
}
