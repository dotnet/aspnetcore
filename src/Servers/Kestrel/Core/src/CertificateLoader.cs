// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Microsoft.AspNetCore.Server.Kestrel.Https;

/// <summary>
/// Enables loading TLS certificates from the certificate store.
/// </summary>
public static class CertificateLoader
{
    // See http://oid-info.com/get/1.3.6.1.5.5.7.3.1
    // Indicates that a certificate can be used as a SSL server certificate
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

    /// <summary>
    /// Loads a certificate from the certificate store.
    /// </summary>
    /// <remarks>
    /// Exact subject match is loaded if present, otherwise best matching certificate with the subject name that contains supplied subject.
    /// Subject comparison is case-insensitive.
    /// </remarks>
    /// <param name="subject">The certificate subject.</param>
    /// <param name="storeName">The certificate store name.</param>
    /// <param name="storeLocation">The certificate store location.</param>
    /// <param name="allowInvalid">Whether or not to load certificates that are considered invalid.</param>
    /// <returns>The loaded certificate.</returns>
    public static X509Certificate2 LoadFromStoreCert(string subject, string storeName, StoreLocation storeLocation, bool allowInvalid)
    {
        using (var store = new X509Store(storeName, storeLocation))
        {
            X509Certificate2Collection? storeCertificates = null;
            X509Certificate2? foundCertificate = null;

            try
            {
                store.Open(OpenFlags.ReadOnly);
                storeCertificates = store.Certificates;
                foreach (var certificate in storeCertificates.Find(X509FindType.FindBySubjectName, subject, !allowInvalid)
                    .OfType<X509Certificate2>()
                    .Where(IsCertificateAllowedForServerAuth)
                    .Where(DoesCertificateHaveAnAccessiblePrivateKey)
                    .OrderByDescending(certificate => certificate.NotAfter))
                {
                    // Pick the first one if there's no exact match as a fallback to substring default.
                    foundCertificate ??= certificate;

                    if (certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false).Equals(subject, StringComparison.InvariantCultureIgnoreCase))
                    {
                        foundCertificate = certificate;
                        break;
                    }
                }

                if (foundCertificate == null)
                {
                    throw new InvalidOperationException(CoreStrings.FormatCertNotFoundInStore(subject, storeLocation, storeName, allowInvalid));
                }

                return foundCertificate;
            }
            finally
            {
                DisposeCertificates(storeCertificates, except: foundCertificate);
            }
        }
    }

    internal static bool IsCertificateAllowedForServerAuth(X509Certificate2 certificate)
    {
        /* If the Extended Key Usage extension is included, then we check that the serverAuth usage is included. (http://oid-info.com/get/1.3.6.1.5.5.7.3.1)
         * If the Extended Key Usage extension is not included, then we assume the certificate is allowed for all usages.
         *
         * See also https://blogs.msdn.microsoft.com/kaushal/2012/02/17/client-certificates-vs-server-certificates/
         *
         * From https://tools.ietf.org/html/rfc3280#section-4.2.1.13 "Certificate Extensions: Extended Key Usage"
         *
         * If the (Extended Key Usage) extension is present, then the certificate MUST only be used
         * for one of the purposes indicated.  If multiple purposes are
         * indicated the application need not recognize all purposes indicated,
         * as long as the intended purpose is present.  Certificate using
         * applications MAY require that a particular purpose be indicated in
         * order for the certificate to be acceptable to that application.
         */

        var hasEkuExtension = false;

        foreach (var extension in certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>())
        {
            hasEkuExtension = true;
            foreach (var oid in extension.EnhancedKeyUsages)
            {
                if (string.Equals(oid.Value, ServerAuthenticationOid, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return !hasEkuExtension;
    }

    internal static bool DoesCertificateHaveAnAccessiblePrivateKey(X509Certificate2 certificate)
        => certificate.HasPrivateKey;

    internal static bool DoesCertificateHaveASubjectAlternativeName(X509Certificate2 certificate)
        => certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().Any();

    private static void DisposeCertificates(X509Certificate2Collection? certificates, X509Certificate2? except)
    {
        if (certificates != null)
        {
            foreach (var certificate in certificates)
            {
                if (!certificate.Equals(except))
                {
                    certificate.Dispose();
                }
            }
        }
    }
}
