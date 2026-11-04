// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Authentication.Certificate;

/// <summary>
/// Extension methods for <see cref="X509Certificate2"/>.
/// </summary>
public static class X509Certificate2Extensions
{
    /// <summary>
    /// Determines if the certificate is self signed.
    /// </summary>
    /// <param name="certificate">The <see cref="X509Certificate2"/>.</param>
    /// <returns>True if the certificate is self signed.</returns>
    public static bool IsSelfSigned(this X509Certificate2 certificate)
    {
        Span<byte> subject = certificate.SubjectName.RawData;
        Span<byte> issuer = certificate.IssuerName.RawData;
        return subject.SequenceEqual(issuer);
    }
}
