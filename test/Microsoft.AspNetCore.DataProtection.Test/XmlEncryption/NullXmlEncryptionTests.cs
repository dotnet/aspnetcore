// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
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
}
