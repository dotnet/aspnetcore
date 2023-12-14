// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

/// <summary>
/// Specifies settings for how to decrypt XML keys.
/// </summary>
internal sealed class XmlKeyDecryptionOptions
{
    private readonly Dictionary<string, List<X509Certificate2>> _certs = new Dictionary<string, List<X509Certificate2>>(StringComparer.Ordinal);

    public int KeyDecryptionCertificateCount => _certs.Count;

    public bool TryGetKeyDecryptionCertificates(X509Certificate2 certInfo, [NotNullWhen(true)] out IReadOnlyList<X509Certificate2>? keyDecryptionCerts)
    {
        var key = GetKey(certInfo);
        var retVal = _certs.TryGetValue(key, out var keyDecryptionCertsRetVal);
        keyDecryptionCerts = keyDecryptionCertsRetVal;
        return retVal;
    }

    public void AddKeyDecryptionCertificate(X509Certificate2 certificate)
    {
        var key = GetKey(certificate);
        if (!_certs.TryGetValue(key, out var certificates))
        {
            certificates = _certs[key] = new List<X509Certificate2>();
        }
        certificates.Add(certificate);
    }

    private static string GetKey(X509Certificate2 cert) => cert.Thumbprint;
}
