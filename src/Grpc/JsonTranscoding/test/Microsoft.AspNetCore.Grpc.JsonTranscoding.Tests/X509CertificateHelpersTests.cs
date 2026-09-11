// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Grpc.Shared;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests;

public class X509CertificateHelpersTests
{
    [Fact]
    public void GetDnsFromExtensions_NoSubjectAlternativeName_ReturnsEmpty()
    {
        using var certificate = CreateCertificate();

        var dnsNames = X509CertificateHelpers.GetDnsFromExtensions(certificate);

        Assert.Empty(dnsNames);
    }

    [Fact]
    public void GetDnsFromExtensions_MixedSubjectAlternativeNames_ReturnsDnsNamesInOrder()
    {
        using var certificate = CreateCertificate(CreateSubjectAlternativeNameExtension(
            (1, "user@example.com"),
            (2, "first.example.com"),
            (6, "https://example.com/"),
            (7, IPAddress.Loopback.GetAddressBytes()),
            (2, "second.example.com")));

        var dnsNames = X509CertificateHelpers.GetDnsFromExtensions(certificate);

        Assert.Equal(["first.example.com", "second.example.com"], dnsNames);
    }

    [Theory]
    [InlineData("first.example, DNS Name=second.example")]
    [InlineData("first.example, DNS:second.example")]
    public void GetDnsFromExtensions_DnsNameContainsFormattedTextDelimiters_ReturnsCompleteValue(string dnsName)
    {
        using var certificate = CreateCertificate(CreateSubjectAlternativeNameExtension((2, dnsName)));

        var dnsNames = X509CertificateHelpers.GetDnsFromExtensions(certificate);

        Assert.Equal([dnsName], dnsNames);
    }

    private static X509Certificate2 CreateCertificate(params X509Extension[] extensions)
    {
        using var key = RSA.Create();
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        foreach (var extension in extensions)
        {
            request.CertificateExtensions.Add(extension);
        }

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(5));
    }

    private static X509Extension CreateSubjectAlternativeNameExtension(params (int Tag, object Value)[] names)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            foreach (var (tag, value) in names)
            {
                var asnTag = new Asn1Tag(TagClass.ContextSpecific, tag);
                if (value is byte[] bytes)
                {
                    writer.WriteOctetString(bytes, asnTag);
                }
                else
                {
                    writer.WriteCharacterString(UniversalTagNumber.IA5String, (string)value, asnTag);
                }
            }
        }

        return new X509Extension(X509CertificateHelpers.X509SubjectAlternativeNameId, writer.Encode(), critical: false);
    }
}
