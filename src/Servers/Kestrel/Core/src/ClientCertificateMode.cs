// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Https;

/// <summary>
/// Describes the client certificate requirements for a HTTPS connection.
/// </summary>
public enum ClientCertificateMode
{
    /// <summary>
    /// A client certificate is not required and will not be requested from clients.
    /// </summary>
    NoCertificate,

    /// <summary>
    /// A client certificate will be requested; however, authentication will not fail if a certificate is not provided by the client.
    /// </summary>
    AllowCertificate,

    /// <summary>
    /// A client certificate will be requested, and the client must provide a valid certificate for authentication to succeed.
    /// </summary>
    RequireCertificate,

    /// <summary>
    /// A client certificate is not required and will not be requested from clients at the start of the connection.
    /// It may be requested by the application later.
    /// </summary>
    DelayCertificate,
}
