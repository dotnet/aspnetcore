// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;

namespace Microsoft.AspNetCore.Internal.Tests;

public class CertificateManagerTests
{
    [Fact]
    public void CreateAspNetCoreHttpsDevelopmentCertificateIsValid()
    {
        var notBefore = DateTimeOffset.Now;
        var notAfter = notBefore.AddMinutes(5);
        var certificate = CertificateManager.Instance.CreateAspNetCoreHttpsDevelopmentCertificate(notBefore, notAfter);

        // Certificate should be valid for the expected time range
        Assert.Equal(notBefore, certificate.NotBefore, TimeSpan.FromSeconds(1));
        Assert.Equal(notAfter, certificate.NotAfter, TimeSpan.FromSeconds(1));

        // Certificate should have a private key
        Assert.True(certificate.HasPrivateKey);

        // Certificate should be recognized as an ASP.NET Core HTTPS development certificate
        Assert.True(CertificateManager.IsHttpsDevelopmentCertificate(certificate));

        // Certificate should include a Subject Key Identifier extension
        var subjectKeyIdentifier = Assert.Single(certificate.Extensions.OfType<X509SubjectKeyIdentifierExtension>());

        // Certificate should include an Authority Key Identifier extension
        var authorityKeyIdentifier = Assert.Single(certificate.Extensions.OfType<X509AuthorityKeyIdentifierExtension>());

        // The Authority Key Identifier should match the Subject Key Identifier
        Assert.True(authorityKeyIdentifier.KeyIdentifier?.Span.SequenceEqual(subjectKeyIdentifier.SubjectKeyIdentifierBytes.Span));
    }

    [Fact]
    public void CreateSelfSignedCertificate_ExistingSubjectKeyIdentifierExtension()
    {
        var subject = new X500DistinguishedName("CN=TestCertificate");
        var notBefore = DateTimeOffset.Now;
        var notAfter = notBefore.AddMinutes(5);
        var testSubjectKeyId = new byte[] { 1, 2, 3, 4, 5 };
        var extensions = new List<X509Extension>
        {
            new X509SubjectKeyIdentifierExtension(testSubjectKeyId, critical: false),
        };

        var certificate = CertificateManager.CreateSelfSignedCertificate(subject, extensions, notBefore, notAfter);

        Assert.Equal(notBefore, certificate.NotBefore, TimeSpan.FromSeconds(1));
        Assert.Equal(notAfter, certificate.NotAfter, TimeSpan.FromSeconds(1));

        // Certificate had an existing Subject Key Identifier extension, so AKID should not be added
        Assert.Empty(certificate.Extensions.OfType<X509AuthorityKeyIdentifierExtension>());

        var subjectKeyIdentifier = Assert.Single(certificate.Extensions.OfType<X509SubjectKeyIdentifierExtension>());
        Assert.True(subjectKeyIdentifier.SubjectKeyIdentifierBytes.Span.SequenceEqual(testSubjectKeyId));
    }

    [Fact]
    public void CreateSelfSignedCertificate_ExistingRawSubjectKeyIdentifierExtension()
    {
        var subject = new X500DistinguishedName("CN=TestCertificate");
        var notBefore = DateTimeOffset.Now;
        var notAfter = notBefore.AddMinutes(5);
        var testSubjectKeyId = new byte[] { 5, 4, 3, 2, 1 };
        // Pass the extension as a raw X509Extension to simulate pre-encoded data
        var extension = new X509SubjectKeyIdentifierExtension(testSubjectKeyId, critical: false);
        var extensions = new List<X509Extension>
        {
            new X509Extension(extension.Oid, extension.RawData, extension.Critical),
        };

        var certificate = CertificateManager.CreateSelfSignedCertificate(subject, extensions, notBefore, notAfter);

        Assert.Equal(notBefore, certificate.NotBefore, TimeSpan.FromSeconds(1));
        Assert.Equal(notAfter, certificate.NotAfter, TimeSpan.FromSeconds(1));

        Assert.Empty(certificate.Extensions.OfType<X509AuthorityKeyIdentifierExtension>());

        var subjectKeyIdentifier = Assert.Single(certificate.Extensions.OfType<X509SubjectKeyIdentifierExtension>());
        Assert.True(subjectKeyIdentifier.SubjectKeyIdentifierBytes.Span.SequenceEqual(testSubjectKeyId));
    }

    [Fact]
    public void CreateSelfSignedCertificate_ExistingRawAuthorityKeyIdentifierExtension()
    {
        var subject = new X500DistinguishedName("CN=TestCertificate");
        var notBefore = DateTimeOffset.Now;
        var notAfter = notBefore.AddMinutes(5);
        var testSubjectKeyId = new byte[] { 9, 8, 7, 6, 5 };
        // Pass the extension as a raw X509Extension to simulate pre-encoded data
        var subjectExtension = new X509SubjectKeyIdentifierExtension(testSubjectKeyId, critical: false);
        var authorityExtension = X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(subjectExtension);
        var extensions = new List<X509Extension>
        {
            new X509Extension(authorityExtension.Oid, authorityExtension.RawData, authorityExtension.Critical),
        };

        var certificate = CertificateManager.CreateSelfSignedCertificate(subject, extensions, notBefore, notAfter);

        Assert.Equal(notBefore, certificate.NotBefore, TimeSpan.FromSeconds(1));
        Assert.Equal(notAfter, certificate.NotAfter, TimeSpan.FromSeconds(1));

        Assert.Empty(certificate.Extensions.OfType<X509SubjectKeyIdentifierExtension>());

        var authorityKeyIdentifier = Assert.Single(certificate.Extensions.OfType<X509AuthorityKeyIdentifierExtension>());
        Assert.True(authorityKeyIdentifier.KeyIdentifier?.Span.SequenceEqual(testSubjectKeyId));
    }

    [Fact]
    public void CreateSelfSignedCertificate_NoSubjectKeyIdentifierExtension()
    {
        var subject = new X500DistinguishedName("CN=TestCertificate");
        var notBefore = DateTimeOffset.Now;
        var notAfter = notBefore.AddMinutes(5);
        var extensions = new List<X509Extension>();

        var certificate = CertificateManager.CreateSelfSignedCertificate(subject, extensions, notBefore, notAfter);

        Assert.Equal(notBefore, certificate.NotBefore, TimeSpan.FromSeconds(1));
        Assert.Equal(notAfter, certificate.NotAfter, TimeSpan.FromSeconds(1));

        var subjectKeyIdentifier = Assert.Single(certificate.Extensions.OfType<X509SubjectKeyIdentifierExtension>());
        var authorityKeyIdentifier = Assert.Single(certificate.Extensions.OfType<X509AuthorityKeyIdentifierExtension>());

        // The Authority Key Identifier should match the Subject Key Identifier
        Assert.True(authorityKeyIdentifier.KeyIdentifier?.Span.SequenceEqual(subjectKeyIdentifier.SubjectKeyIdentifierBytes.Span));
    }
}