// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.CertificateGeneration.Task
{
    internal static class CertificateManager
    {
        public static X509Certificate2 GenerateSSLCertificate(
            string subjectName,
            IEnumerable<string> subjectAlternativeName,
            string friendlyName,
            DateTimeOffset notBefore,
            DateTimeOffset expires,
            StoreName storeName,
            StoreLocation storeLocation)
        {
            using (var rsa = RSA.Create(2048))
            {
                var signingRequest = new CertificateRequest(
                    new X500DistinguishedName(subjectName), rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var enhancedKeyUsage = new OidCollection();
                enhancedKeyUsage.Add(new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication"));
                signingRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsage, critical: true));
                signingRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, critical: true));
                signingRequest.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(
                        certificateAuthority: false,
                        hasPathLengthConstraint: false,
                        pathLengthConstraint: 0,
                        critical: true));

                var sanBuilder = new SubjectAlternativeNameBuilder();
                foreach (var alternativeName in subjectAlternativeName)
                {
                    sanBuilder.AddDnsName(alternativeName);
                }
                signingRequest.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = signingRequest.CreateSelfSigned(notBefore, expires);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    certificate.FriendlyName = friendlyName;
                }

                SaveCertificate(storeName, storeLocation, certificate);

                return certificate;
            }
        }

        private static void SaveCertificate(StoreName storeName, StoreLocation storeLocation, X509Certificate2 certificate)
        {
            // We need to take this step so that the key gets persisted.
            var imported = certificate;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var export = certificate.Export(X509ContentType.Pkcs12, "");
                imported = new X509Certificate2(export, "", X509KeyStorageFlags.PersistKeySet);
                Array.Clear(export, 0, export.Length);
            }

            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(imported);
                store.Close();
            };
        }

        public static X509Certificate2 FindCertificate(string subjectValue, StoreName storeName, StoreLocation storeLocation)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                var certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subjectValue, validOnly: false);
                var current = DateTimeOffset.UtcNow;

                var found = certificates.OfType<X509Certificate2>()
                    .Where(c => c.NotBefore <= current && current <= c.NotAfter && c.HasPrivateKey)
                    .FirstOrDefault();
                store.Close();

                return found;
            };
        }
    }
}
