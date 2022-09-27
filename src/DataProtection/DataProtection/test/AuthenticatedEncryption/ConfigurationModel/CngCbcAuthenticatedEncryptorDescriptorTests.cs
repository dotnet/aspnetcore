// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

public class CngCbcAuthenticatedEncryptorDescriptorTests
{
    [Fact]
    public void ExportToXml_WithProviders_ProducesCorrectPayload()
    {
        // Arrange
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));
        var descriptor = new CngCbcAuthenticatedEncryptorDescriptor(new CngCbcAuthenticatedEncryptorConfiguration()
        {
            EncryptionAlgorithm = "enc-alg",
            EncryptionAlgorithmKeySize = 2048,
            EncryptionAlgorithmProvider = "enc-alg-prov",
            HashAlgorithm = "hash-alg",
            HashAlgorithmProvider = "hash-alg-prov"
        }, masterKey.ToSecret());

        // Act
        var retVal = descriptor.ExportToXml();

        // Assert
        Assert.Equal(typeof(CngCbcAuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
        var expectedXml = $@"
                <descriptor>
                  <encryption algorithm='enc-alg' keyLength='2048' provider='enc-alg-prov' />
                  <hash algorithm='hash-alg' provider='hash-alg-prov' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>{masterKey}</value>
                  </masterKey>
                </descriptor>";
        XmlAssert.Equal(expectedXml, retVal.SerializedDescriptorElement);
    }

    [Fact]
    public void ExportToXml_WithoutProviders_ProducesCorrectPayload()
    {
        // Arrange
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));
        var descriptor = new CngCbcAuthenticatedEncryptorDescriptor(new CngCbcAuthenticatedEncryptorConfiguration()
        {
            EncryptionAlgorithm = "enc-alg",
            EncryptionAlgorithmKeySize = 2048,
            HashAlgorithm = "hash-alg"
        }, masterKey.ToSecret());

        // Act
        var retVal = descriptor.ExportToXml();

        // Assert
        Assert.Equal(typeof(CngCbcAuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
        var expectedXml = $@"
                <descriptor>
                  <encryption algorithm='enc-alg' keyLength='2048' />
                  <hash algorithm='hash-alg' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>{masterKey}</value>
                  </masterKey>
                </descriptor>";
        XmlAssert.Equal(expectedXml, retVal.SerializedDescriptorElement);
    }
}
