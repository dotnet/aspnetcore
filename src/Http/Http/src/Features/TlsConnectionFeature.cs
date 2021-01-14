// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
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
}
