// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    public class CngCbcAuthenticatedEncryptorDescriptorTests
    {
        [Fact]
        public void ExportToXml_WithProviders_ProducesCorrectPayload()
        {
            // Arrange
            var masterKey = "k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==".ToSecret();
            var descriptor = new CngCbcAuthenticatedEncryptorDescriptor(new CngCbcAuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithm = "enc-alg",
                EncryptionAlgorithmKeySize = 2048,
                EncryptionAlgorithmProvider = "enc-alg-prov",
                HashAlgorithm = "hash-alg",
                HashAlgorithmProvider = "hash-alg-prov"
            }, masterKey);

            // Act
            var retVal = descriptor.ExportToXml();

            // Assert
            Assert.Equal(typeof(CngCbcAuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
            const string expectedXml = @"
                <descriptor>
                  <encryption algorithm='enc-alg' keyLength='2048' provider='enc-alg-prov' />
                  <hash algorithm='hash-alg' provider='hash-alg-prov' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==</value>
                  </masterKey>
                </descriptor>";
            XmlAssert.Equal(expectedXml, retVal.SerializedDescriptorElement);
        }

        [Fact]
        public void ExportToXml_WithoutProviders_ProducesCorrectPayload()
        {
            // Arrange
            var masterKey = "k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==".ToSecret();
            var descriptor = new CngCbcAuthenticatedEncryptorDescriptor(new CngCbcAuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithm = "enc-alg",
                EncryptionAlgorithmKeySize = 2048,
                HashAlgorithm = "hash-alg"
            }, masterKey);

            // Act
            var retVal = descriptor.ExportToXml();

            // Assert
            Assert.Equal(typeof(CngCbcAuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
            const string expectedXml = @"
                <descriptor>
                  <encryption algorithm='enc-alg' keyLength='2048' />
                  <hash algorithm='hash-alg' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==</value>
                  </masterKey>
                </descriptor>";
            XmlAssert.Equal(expectedXml, retVal.SerializedDescriptorElement);
        }
    }
}
