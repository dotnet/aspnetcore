// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Certificate;

/// <summary>
/// Enum representing certificate types.
/// </summary>
[Flags]
public enum CertificateTypes
{
    /// <summary>
    /// Chained certificates.
    /// </summary>
    Chained = 1,

    /// <summary>
    /// SelfSigned certificates.
    /// </summary>
    SelfSigned = 2,

    /// <summary>
    /// All certificates.
    /// </summary>
    All = Chained | SelfSigned
}
