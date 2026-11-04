// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Certificate;

/// <summary>
/// Default values related to certificate authentication middleware
/// </summary>
public static class CertificateAuthenticationDefaults
{
    /// <summary>
    /// The default value used for CertificateAuthenticationOptions.AuthenticationScheme
    /// </summary>
    public const string AuthenticationScheme = "Certificate";
}
