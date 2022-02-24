// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

public class CngGcmAuthenticatedEncryptorDescriptorTests
{
    [Fact]
    public void ExportToXml_WithProviders_ProducesCorrectPayload()
    {
        // Arrange
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));
        var descriptor = new CngGcmAuthenticatedEncryptorDescriptor(new CngGcmAuthenticatedEncryptorConfiguration()
        {
            EncryptionAlgorithm = "enc-alg",
            EncryptionAlgorithmKeySize = 2048,
            EncryptionAlgorithmProvider = "enc-alg-prov"
        }, masterKey.ToSecret());

        // Act
        var retVal = descriptor.ExportToXml();

        // Assert
        Assert.Equal(typeof(CngGcmAuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
        var expectedXml = $@"
                <descriptor>
                  <encryption algorithm='enc-alg' keyLength='2048' provider='enc-alg-prov' />
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
        var descriptor = new CngGcmAuthenticatedEncryptorDescriptor(new CngGcmAuthenticatedEncryptorConfiguration()
        {
            EncryptionAlgorithm = "enc-alg",
            EncryptionAlgorithmKeySize = 2048
        }, masterKey.ToSecret());

        // Act
        var retVal = descriptor.ExportToXml();

        // Assert
        Assert.Equal(typeof(CngGcmAuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
        var expectedXml = $@"
                <descriptor>
                  <encryption algorithm='enc-alg' keyLength='2048' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>{masterKey}</value>
                  </masterKey>
                </descriptor>";
        XmlAssert.Equal(expectedXml, retVal.SerializedDescriptorElement);
    }
}
