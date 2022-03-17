// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Certificate;

/// <summary>
/// Context used when certificates are being validated.
/// </summary>
public class CertificateValidatedContext : ResultContext<CertificateAuthenticationOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="CertificateValidatedContext"/>.
    /// </summary>
    /// <param name="context">The HttpContext the validate context applies too.</param>
    /// <param name="scheme">The scheme used when the Certificate Authentication handler was registered.</param>
    /// <param name="options">The <see cref="CertificateAuthenticationOptions"/>.</param>
    public CertificateValidatedContext(
        HttpContext context,
        AuthenticationScheme scheme,
        CertificateAuthenticationOptions options)
        : base(context, scheme, options)
    {
    }

    /// <summary>
    /// The certificate to validate.
    /// </summary>
    public X509Certificate2 ClientCertificate { get; set; } = default!;
}
