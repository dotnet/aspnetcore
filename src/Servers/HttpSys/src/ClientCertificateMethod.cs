// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.HttpSys
{
    /// <summary>
    /// Describes the client certificate negotiation method for HTTPS connections.
    /// </summary>
    public enum ClientCertificateMethod
    {
        /// <summary>
        /// A client certificate will not be populated on the request.
        /// </summary>
        NoCertificate = 0,

        /// <summary>
        /// A client certificate will be populated if already present at the start of a request.
        /// </summary>
        AllowCertificate,

        /// <summary>
        /// The TLS session can be renegotiated to request a client certificate.
        /// </summary>
        AllowRenegotation
    }
}
