// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

public class CertificateXmlEncryptorTests
{
    [Fact]
    public void Encrypt_Decrypt_RoundTrips()
    {
        // Arrange
        var symmetricAlgorithm = TripleDES.Create();
        symmetricAlgorithm.GenerateKey();

        var mockInternalEncryptor = new Mock<IInternalCertificateXmlEncryptor>();
        mockInternalEncryptor.Setup(o => o.PerformEncryption(It.IsAny<EncryptedXml>(), It.IsAny<XmlElement>()))
            .Returns<EncryptedXml, XmlElement>((encryptedXml, element) =>
            {
                encryptedXml.AddKeyNameMapping("theKey", symmetricAlgorithm); // use symmetric encryption
                return encryptedXml.Encrypt(element, "theKey");
            });

        var mockInternalDecryptor = new Mock<IInternalEncryptedXmlDecryptor>();
        mockInternalDecryptor.Setup(o => o.PerformPreDecryptionSetup(It.IsAny<EncryptedXml>()))
            .Callback<EncryptedXml>(encryptedXml =>
            {
                encryptedXml.AddKeyNameMapping("theKey", symmetricAlgorithm); // use symmetric encryption
            });

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IInternalEncryptedXmlDecryptor>(mockInternalDecryptor.Object);

        var services = serviceCollection.BuildServiceProvider();
        var encryptor = new CertificateXmlEncryptor(NullLoggerFactory.Instance, mockInternalEncryptor.Object);
        var decryptor = new EncryptedXmlDecryptor(services);

        var originalXml = XElement.Parse(@"<mySecret value='265ee4ea-ade2-43b1-b706-09b259e58b6b' />");

        // Act & assert - run through encryptor and make sure we get back <EncryptedData> element
        var encryptedXmlInfo = encryptor.Encrypt(originalXml);
        Assert.Equal(typeof(EncryptedXmlDecryptor), encryptedXmlInfo.DecryptorType);
        Assert.Equal(XName.Get("EncryptedData", "http://www.w3.org/2001/04/xmlenc#"), encryptedXmlInfo.EncryptedElement.Name);
        Assert.Equal("http://www.w3.org/2001/04/xmlenc#Element", (string)encryptedXmlInfo.EncryptedElement.Attribute("Type"));
        Assert.DoesNotContain("265ee4ea-ade2-43b1-b706-09b259e58b6b", encryptedXmlInfo.EncryptedElement.ToString(), StringComparison.OrdinalIgnoreCase);

        // Act & assert - run through decryptor and make sure we get back the original value
        var roundTrippedElement = decryptor.Decrypt(encryptedXmlInfo.EncryptedElement);
        XmlAssert.Equal(originalXml, roundTrippedElement);
    }
}
