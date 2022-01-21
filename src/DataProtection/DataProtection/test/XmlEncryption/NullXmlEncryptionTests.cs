// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

public class NullXmlEncryptionTests
{
    [Fact]
    public void NullDecryptor_ReturnsOriginalElement()
    {
        // Arrange
        var decryptor = new NullXmlDecryptor();

        // Act
        var retVal = decryptor.Decrypt(XElement.Parse("<unencryptedKey><theElement /></unencryptedKey>"));

        // Assert
        XmlAssert.Equal("<theElement />", retVal);
    }

    [Fact]
    public void NullEncryptor_ReturnsOriginalElement()
    {
        // Arrange
        var encryptor = new NullXmlEncryptor();

        // Act
        var retVal = encryptor.Encrypt(XElement.Parse("<theElement />"));

        // Assert
        Assert.Equal(typeof(NullXmlDecryptor), retVal.DecryptorType);
        XmlAssert.Equal("<unencryptedKey><theElement /></unencryptedKey>", retVal.EncryptedElement);
    }
}
