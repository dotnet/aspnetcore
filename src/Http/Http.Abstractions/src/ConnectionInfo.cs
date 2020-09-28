// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Gets information about the current connection.
    /// </summary>
    public abstract class ConnectionInfo
    {
        /// <summary>
        /// Gets or sets a unique identifier to represent this connection.
        /// </summary>
        public abstract string Id { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IPAddress"/> for the connecting client.
        /// </summary>
        public abstract IPAddress? RemoteIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the port for the connecting client.
        /// </summary>
        public abstract int RemotePort { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IPAddress"/> for the host.
        /// </summary>
        public abstract IPAddress? LocalIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the port for the host.
        /// </summary>
        public abstract int LocalPort { get; set; }

        /// <summary>
        /// Gets or sets the client certificate associated with the connection.
        /// </summary>
        public abstract X509Certificate2? ClientCertificate { get; set; }

        /// <summary>
        /// Asynchronously retrieves the client certificate.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The client certificate if available.</returns>
        public abstract Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken = new CancellationToken());
    }
}
