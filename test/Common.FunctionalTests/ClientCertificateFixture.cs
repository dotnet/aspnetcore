// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class ClientCertificateFixture : IDisposable
    {
        private X509Certificate2 _certificate;

        public X509Certificate2 Certificate
        {
            get
            {
                if (_certificate != null)
                {
                    return _certificate;
                }

                using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadWrite);

                    foreach (var cert in store.Certificates)
                    {
                        if (cert.Issuer != "CN=IISIntegrationTest_Root")
                        {
                            continue;
                        }
                        _certificate = cert;
                        store.Close();
                        return cert;
                    }

                    var parentKey = CreateKeyMaterial(2048);

                    // On first run of the test, creates the certificate in the trusted root certificate authorities.
                    var parentRequest = new CertificateRequest("CN=IISIntegrationTest_Root", parentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    parentRequest.CertificateExtensions.Add(
                        new X509BasicConstraintsExtension(
                            certificateAuthority: true,
                            hasPathLengthConstraint: false,
                            pathLengthConstraint: 0,
                            critical: true));

                    parentRequest.CertificateExtensions.Add(
                        new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, critical: true));

                    parentRequest.CertificateExtensions.Add(
                        new X509SubjectKeyIdentifierExtension(parentRequest.PublicKey, false));

                    var notBefore = DateTimeOffset.Now.AddDays(-1);
                    var notAfter = DateTimeOffset.Now.AddYears(5);

                    var parentCert = parentRequest.CreateSelfSigned(notBefore, notAfter);

                    // Need to export/import the certificate to associate the private key with the cert.
                    var imported = parentCert;

                    var export = parentCert.Export(X509ContentType.Pkcs12, "");
                    imported = new X509Certificate2(export, "", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                    Array.Clear(export, 0, export.Length);

                    // Add the cert to the cert store
                    _certificate = imported;

                    store.Add(certificate: imported);
                    store.Close();
                    return imported;
                }
            }
        }

        public void Dispose()
        {
            if (_certificate == null)
            {
                return;
            }

            using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Remove(Certificate);
                store.Close();
            }
        }

        private RSA CreateKeyMaterial(int minimumKeySize)
        {
            var rsa = RSA.Create(minimumKeySize);
            if (rsa.KeySize < minimumKeySize)
            {
                throw new InvalidOperationException($"Failed to create a key with a size of {minimumKeySize} bits");
            }

            return rsa;
        }
    }
}
