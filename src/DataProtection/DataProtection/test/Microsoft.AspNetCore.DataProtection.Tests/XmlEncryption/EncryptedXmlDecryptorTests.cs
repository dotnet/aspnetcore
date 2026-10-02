// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.InternalTesting;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.Test.XmlEncryption;

public class EncryptedXmlDecryptorTests
{
    [Fact]
    public void ThrowsIfCannotDecrypt()
    {
        using var testCert1 = TestCertificateFactory.CreateRsaCertificate();
        var encryptor = new CertificateXmlEncryptor(testCert1, NullLoggerFactory.Instance);
        var data = new XElement("SampleData", "Lorem ipsum");
        var encryptedXml = encryptor.Encrypt(data);
        var decryptor = new EncryptedXmlDecryptor();

        var ex = Assert.Throws<CryptographicException>(() =>
            decryptor.Decrypt(encryptedXml.EncryptedElement));
        Assert.Equal("Unable to retrieve the decryption key.", ex.Message);
    }

    [Fact]
    public void ThrowsIfProvidedCertificateDoesNotMatch()
    {
        using var testCert1 = TestCertificateFactory.CreateRsaCertificate();
        using var testCert2 = TestCertificateFactory.CreateRsaCertificate();
        var services = new ServiceCollection()
            .Configure<XmlKeyDecryptionOptions>(o => o.AddKeyDecryptionCertificate(testCert2))
            .BuildServiceProvider();
        var encryptor = new CertificateXmlEncryptor(testCert1, NullLoggerFactory.Instance);
        var data = new XElement("SampleData", "Lorem ipsum");
        var encryptedXml = encryptor.Encrypt(data);
        var decryptor = new EncryptedXmlDecryptor(services);

        var ex = Assert.Throws<CryptographicException>(() =>
                decryptor.Decrypt(encryptedXml.EncryptedElement));
        Assert.Equal("Unable to retrieve the decryption key.", ex.Message);
    }

    [Fact]
    public void ThrowsIfProvidedCertificateDoesHavePrivateKey()
    {
        using var fullCert = TestCertificateFactory.CreateRsaCertificate();
        using var publicKeyOnly = TestCertificateFactory.CreatePublicKeyOnlyCertificate(fullCert);
        var services = new ServiceCollection()
            .Configure<XmlKeyDecryptionOptions>(o => o.AddKeyDecryptionCertificate(publicKeyOnly))
            .BuildServiceProvider();
        var encryptor = new CertificateXmlEncryptor(fullCert, NullLoggerFactory.Instance);
        var data = new XElement("SampleData", "Lorem ipsum");
        var encryptedXml = encryptor.Encrypt(data);
        var decryptor = new EncryptedXmlDecryptor(services);

        var ex = Assert.Throws<CryptographicException>(() =>
                decryptor.Decrypt(encryptedXml.EncryptedElement));
        Assert.Equal("Unable to retrieve the decryption key.", ex.Message);
    }

    [Fact]
    public void XmlCanRoundTrip()
    {
        using var testCert1 = TestCertificateFactory.CreateRsaCertificate();
        using var testCert2 = TestCertificateFactory.CreateRsaCertificate();
        var services = new ServiceCollection()
            .Configure<XmlKeyDecryptionOptions>(o =>
            {
                o.AddKeyDecryptionCertificate(testCert1);
                o.AddKeyDecryptionCertificate(testCert2);
            })
            .BuildServiceProvider();
        var encryptor = new CertificateXmlEncryptor(testCert1, NullLoggerFactory.Instance);
        var data = new XElement("SampleData", "Lorem ipsum");
        var encryptedXml = encryptor.Encrypt(data);
        var decryptor = new EncryptedXmlDecryptor(services);

        var decrypted = decryptor.Decrypt(encryptedXml.EncryptedElement);

        Assert.Equal("SampleData", decrypted.Name);
        Assert.Equal("Lorem ipsum", decrypted.Value);
    }
}
