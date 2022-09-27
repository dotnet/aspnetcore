// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.HttpOverrides;

/// <summary>
/// Used to configure the <see cref="CertificateForwardingMiddleware"/>.
/// </summary>
public class CertificateForwardingOptions
{
    /// <summary>
    /// The name of the header containing the client certificate.
    /// </summary>
    /// <remarks>
    /// This defaults to X-Client-Cert
    /// </remarks>
    public string CertificateHeader { get; set; } = "X-Client-Cert";

    /// <summary>
    /// The function used to convert the header to an instance of <see cref="X509Certificate2"/>.
    /// </summary>
    /// <remarks>
    /// This defaults to a conversion from a base64 encoded string.
    /// </remarks>
    public Func<string, X509Certificate2> HeaderConverter = (headerValue) => new X509Certificate2(Convert.FromBase64String(headerValue));
}
