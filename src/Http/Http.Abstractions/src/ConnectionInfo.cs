// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents the underlying connection for a request.
/// </summary>
public abstract class ConnectionInfo
{
    /// <summary>
    /// Gets or sets a unique identifier to represent this connection.
    /// </summary>
    public abstract string Id { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the remote target. Can be null.
    /// </summary>
    public abstract IPAddress? RemoteIpAddress { get; set; }

    /// <summary>
    /// Gets or sets the port of the remote target.
    /// </summary>
    public abstract int RemotePort { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the local host.
    /// </summary>
    public abstract IPAddress? LocalIpAddress { get; set; }

    /// <summary>
    /// Gets or sets the port of the local host.
    /// </summary>
    public abstract int LocalPort { get; set; }

    /// <summary>
    /// Gets or sets the client certificate.
    /// </summary>
    public abstract X509Certificate2? ClientCertificate { get; set; }

    /// <summary>
    /// Retrieves the client certificate.
    /// </summary>
    /// <returns>Asynchronously returns an <see cref="X509Certificate2" />. Can be null.</returns>
    public abstract Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken = new CancellationToken());

    /// <summary>
    /// Close connection gracefully.
    /// </summary>
    public virtual void RequestClose()
    {
    }
}
