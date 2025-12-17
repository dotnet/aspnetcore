// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

/// <summary>
/// Options for DirectSsl transport.
/// </summary>
public class DirectSslTransportOptions
{
    /// <summary>
    /// Path to the PEM-encoded certificate file.
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Path to the PEM-encoded private key file.
    /// </summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>
    /// The number of SSL worker threads for handling TLS handshakes.
    /// </summary>
    /// <remarks>
    /// Defaults to 4.
    /// </remarks>
    public int WorkerCount { get; set; } = 4;
}
