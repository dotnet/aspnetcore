// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Certificate;

/// <summary>
/// State for the Chain Build event.
/// </summary>
public class CertificateValidatingContext : BaseContext<CertificateAuthenticationOptions>
{
    /// <summary>
    /// Creates a new <see cref="CertificateValidatingContext"/>.
    /// </summary>
    /// <param name="context">The HttpContext the validate context applies too.</param>
    /// <param name="scheme">The scheme used when the Certificate Authentication handler was registered.</param>
    /// <param name="options">The <see cref="CertificateAuthenticationOptions"/>.</param>
    public CertificateValidatingContext(
        HttpContext context,
        AuthenticationScheme scheme,
        CertificateAuthenticationOptions options)
        : base(context, scheme, options) { }

    /// <summary>
    /// Gets or sets the policy to be applied to the client certificate chain.
    /// </summary>
    public X509ChainPolicy ChainPolicy { get; set; } = default!;

    /// <summary>
    /// Gets or sets the certificate that will be validated.
    /// </summary>
    public X509Certificate2 ClientCertificate { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value that indicates whether the client certificate is self-signed.
    /// </summary>
    public bool IsSelfSigned { get; set; }
}
