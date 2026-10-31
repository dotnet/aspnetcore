// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Certificate;

/// <summary>
/// State for the Challenge event.
/// </summary>
public class CertificateChallengeContext : PropertiesContext<CertificateAuthenticationOptions>
{
    /// <summary>
    /// Creates a new <see cref="CertificateChallengeContext"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="scheme"></param>
    /// <param name="options"></param>
    /// <param name="properties"></param>
    public CertificateChallengeContext(
        HttpContext context,
        AuthenticationScheme scheme,
        CertificateAuthenticationOptions options,
        AuthenticationProperties properties)
        : base(context, scheme, options, properties) { }

    /// <summary>
    /// If true, will skip any default logic for this challenge.
    /// </summary>
    public bool Handled { get; private set; }

    /// <summary>
    /// Skips any default logic for this challenge.
    /// </summary>
    public void HandleResponse() => Handled = true;
}
