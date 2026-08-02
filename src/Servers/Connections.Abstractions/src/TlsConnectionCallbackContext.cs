// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP
using System.Net.Security;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Per connection state used to determine the TLS options.
/// </summary>
public class TlsConnectionCallbackContext
{
    /// <summary>
    /// Information from the Client Hello message.
    /// </summary>
    public SslClientHelloInfo ClientHelloInfo { get; set; }

    /// <summary>
    /// The information that was passed when registering the callback.
    /// </summary>
    public object? State { get; set; }

    /// <summary>
    /// Information about an individual connection.
    /// </summary>
    public BaseConnectionContext Connection { get; set; } = default!;
}
#endif
