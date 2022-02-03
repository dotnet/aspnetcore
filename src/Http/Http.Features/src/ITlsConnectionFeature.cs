// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides access to TLS features associated with the current HTTP connection.
/// </summary>
public interface ITlsConnectionFeature
{
    /// <summary>
    /// Synchronously retrieves the client certificate, if any.
    /// </summary>
    X509Certificate2? ClientCertificate { get; set; }

    /// <summary>
    /// Asynchronously retrieves the client certificate, if any.
    /// </summary>
    Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken);
}
