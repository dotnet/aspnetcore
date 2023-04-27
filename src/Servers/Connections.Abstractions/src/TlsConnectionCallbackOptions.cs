// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Options used to configure a per connection callback for TLS configuration.
/// </summary>
public class TlsConnectionCallbackOptions
{
    /// <summary>
    /// The callback to invoke per connection. This property is required.
    /// </summary>
    public Func<TlsConnectionCallbackContext, CancellationToken, ValueTask<SslServerAuthenticationOptions>> OnConnection { get; set; } = default!;

    /// <summary>
    /// Optional application state to flow to the <see cref="OnConnection"/> callback.
    /// </summary>
    public object? OnConnectionState { get; set; }

    /// <summary>
    /// Gets or sets a list of ALPN protocols.
    /// </summary>
    public List<SslApplicationProtocol> ApplicationProtocols { get; set; } = default!;
}
#endif
