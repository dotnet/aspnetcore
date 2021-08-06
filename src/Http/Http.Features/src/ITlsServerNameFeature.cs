// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides access to the Server Name as presented by the client associated with the current HTTP connection.
    /// </summary>
    public interface ITlsServerNameFeature
    {
        /// <summary>
        /// Gets the hostname presented by the client during the TLS handshake.
        /// </summary>
        public string? ServerName { get; }
    }
}
