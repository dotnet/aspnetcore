// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Https;

/// <summary>
/// Per connection state used to determine the TLS options.
/// </summary>
public class TlsHandshakeCallbackContext
{
    // ServerOptionsSelectionCallback parameters

    /// <summary>
    /// The TLS stream on which the authentication happens.
    /// </summary>
    public SslStream SslStream { get; internal set; } = default!;

    /// <summary>
    /// Information from the Client Hello message.
    /// </summary>
    public SslClientHelloInfo ClientHelloInfo { get; internal set; }

    /// <summary>
    /// The information that was passed when registering the callback.
    /// </summary>
    public object? State { get; internal set; }

    /// <summary>
    /// The token to monitor for cancellation requests.
    /// </summary>
    public CancellationToken CancellationToken { get; internal set; }

    // Kestrel specific

    /// <summary>
    /// Information about an individual connection.
    /// </summary>
    public ConnectionContext Connection { get; internal set; } = default!;

    /// <summary>
    /// Indicates if the application is allowed to request a client certificate after the handshake has completed.
    /// The default is false. See <see cref="ITlsConnectionFeature.GetClientCertificateAsync"/>
    /// </summary>
    public bool AllowDelayedClientCertificateNegotation { get; set; }
}
