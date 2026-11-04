// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Authentication.Certificate;

public static class Certificates
{
    private static string ServerEku = "1.3.6.1.5.5.7.3.1";
    private static string ClientEku = "1.3.6.1.5.5.7.3.2";

    static Certificates()
    {
        var now = TimeProvider.System.GetUtcNow();

        SelfSignedPrimaryRoot = MakeCert(
            "CN=Valid Self Signed Client EKU,OU=dev,DC=idunno-dev,DC=org",
            ClientEku,
            now);

        SignedSecondaryRoot = MakeCert(
            "CN=Valid Signed Secondary Root EKU,OU=dev,DC=idunno-dev,DC=org",
            ClientEku,
            now);

        SelfSignedValidWithServerEku = MakeCert(
            "CN=Valid Self Signed Server EKU,OU=dev,DC=idunno-dev,DC=org",
            ServerEku,
            now);

        SelfSignedValidWithClientEku = MakeCert(
            "CN=Valid Self Signed Server EKU,OU=dev,DC=idunno-dev,DC=org",
            ClientEku,
            now);

        SelfSignedValidWithNoEku = MakeCert(
            "CN=Valid Self Signed No EKU,OU=dev,DC=idunno-dev,DC=org",
            eku: null,
            now);

        SelfSignedExpired = MakeCert(
            "CN=Expired Self Signed,OU=dev,DC=idunno-dev,DC=org",
            eku: null,
            now.AddYears(-2),
            now.AddYears(-1));

        SelfSignedNotYetValid = MakeCert(
            "CN=Not Valid Yet Self Signed,OU=dev,DC=idunno-dev,DC=org",
            eku: null,
            now.AddYears(2),
            now.AddYears(3));

        SignedClient = MakeCert(
            "CN=Valid Signed Client,OU=dev,DC=idunno-dev,DC=org",
            ClientEku,
            now);

    }

    private static readonly X509KeyUsageExtension s_digitalSignatureOnlyUsage =
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true);

    private static X509Certificate2 MakeCert(
        string subjectName,
        string eku,
        DateTimeOffset now)
    {
        return MakeCert(subjectName, eku, now, now.AddYears(5));
    }

    private static X509Certificate2 MakeCert(
        string subjectName,
        string eku,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter)
    {
        using (var key = RSA.Create(2048))
        {
            CertificateRequest request = new CertificateRequest(
                subjectName,
                key,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(s_digitalSignatureOnlyUsage);

            if (eku != null)
            {
                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection { new Oid(eku, null) }, false));
            }

            return request.CreateSelfSigned(notBefore, notAfter);
        }
    }

    public static X509Certificate2 SelfSignedPrimaryRoot { get; private set; }

    public static X509Certificate2 SignedSecondaryRoot { get; private set; }

    public static X509Certificate2 SignedClient { get; private set; }

    public static X509Certificate2 SelfSignedValidWithClientEku { get; private set; }

    public static X509Certificate2 SelfSignedValidWithNoEku { get; private set; }

    public static X509Certificate2 SelfSignedValidWithServerEku { get; private set; }

    public static X509Certificate2 SelfSignedNotYetValid { get; private set; }

    public static X509Certificate2 SelfSignedExpired { get; private set; }
}
