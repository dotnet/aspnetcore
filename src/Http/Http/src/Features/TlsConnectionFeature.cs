// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Default implementation for <see cref="TlsConnectionFeature"/>.
/// </summary>
public class TlsConnectionFeature : ITlsConnectionFeature
{
    /// <inheritdoc />
    public X509Certificate2? ClientCertificate { get; set; }

    /// <inheritdoc />
    public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ClientCertificate);
    }
}
