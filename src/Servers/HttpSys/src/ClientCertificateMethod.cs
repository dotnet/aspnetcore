// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

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
