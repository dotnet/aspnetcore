// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.Test.XmlEncryption;

public class EncryptedXmlDecryptorTests
{
    [Fact]
    public void ThrowsIfCannotDecrypt()
    {
        var testCert1 = new X509Certificate2(Path.Combine(AppContext.BaseDirectory, "TestFiles", "TestCert1.pfx"), "password");
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
        var testCert1 = new X509Certificate2(Path.Combine(AppContext.BaseDirectory, "TestFiles", "TestCert1.pfx"), "password");
        var testCert2 = new X509Certificate2(Path.Combine(AppContext.BaseDirectory, "TestFiles", "TestCert2.pfx"), "password");
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
        var fullCert = new X509Certificate2(Path.Combine(AppContext.BaseDirectory, "TestFiles", "TestCert1.pfx"), "password");
        var publicKeyOnly = new X509Certificate2(Path.Combine(AppContext.BaseDirectory, "TestFiles", "TestCert1.PublicKeyOnly.cer"), "");
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
        var testCert1 = new X509Certificate2(Path.Combine(AppContext.BaseDirectory, "TestFiles", "TestCert1.pfx"), "password");
        var testCert2 = new X509Certificate2(Path.Combine(AppContext.BaseDirectory, "TestFiles", "TestCert2.pfx"), "password");
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
