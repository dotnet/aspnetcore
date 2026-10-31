// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

public class ClientCertificateFixture : IDisposable
{
    private X509Certificate2 _certificate;
    private const string _certIssuerPrefix = "CN=IISIntegrationTest_Root";

    public X509Certificate2 GetOrCreateCertificate()
    {
        if (_certificate != null)
        {
            return _certificate;
        }

        using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadWrite);
            var parentKey = CreateKeyMaterial(2048);

            // Create a cert name with a random guid to avoid name conflicts
            var parentRequest = new CertificateRequest(
                _certIssuerPrefix + Guid.NewGuid().ToString(),
                parentKey, HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

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

    public void Dispose()
    {
        if (_certificate == null)
        {
            return;
        }

        using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadWrite);
            store.Remove(_certificate);

            // Remove any extra certs that were left by previous tests.
            for (var i = store.Certificates.Count - 1; i >= 0; i--)
            {
                var cert = store.Certificates[i];
                if (cert.Issuer.StartsWith(_certIssuerPrefix, StringComparison.Ordinal))
                {
                    store.Remove(cert);
                }
            }
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
