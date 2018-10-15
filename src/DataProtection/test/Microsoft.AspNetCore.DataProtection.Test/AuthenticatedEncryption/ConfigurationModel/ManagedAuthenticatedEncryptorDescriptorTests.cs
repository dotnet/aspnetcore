// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    public class ManagedAuthenticatedEncryptorDescriptorTests
    {
        [Fact]
        public void ExportToXml_CustomTypes_ProducesCorrectPayload()
        {
            // Arrange
            var masterKey = "k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==".ToSecret();
            var descriptor = new ManagedAuthenticatedEncryptorDescriptor(new ManagedAuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithmType = typeof(MySymmetricAlgorithm),
                EncryptionAlgorithmKeySize = 2048,
                ValidationAlgorithmType = typeof(MyKeyedHashAlgorithm)
            }, masterKey);

            // Act
            var retVal = descriptor.ExportToXml();

            // Assert
            Assert.Equal(typeof(ManagedAuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
            string expectedXml = string.Format(@"
                <descriptor>
                  <encryption algorithm='{0}' keyLength='2048' />
                  <validation algorithm='{1}' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==</value>
                  </masterKey>
                </descriptor>",
                typeof(MySymmetricAlgorithm).AssemblyQualifiedName, typeof(MyKeyedHashAlgorithm).AssemblyQualifiedName);
            XmlAssert.Equal(expectedXml, retVal.SerializedDescriptorElement);
        }

        [Theory]
        [InlineData(typeof(Aes), typeof(HMACSHA1))]
        [InlineData(typeof(Aes), typeof(HMACSHA256))]
        [InlineData(typeof(Aes), typeof(HMACSHA384))]
        [InlineData(typeof(Aes), typeof(HMACSHA512))]
        public void ExportToXml_BuiltInTypes_ProducesCorrectPayload(Type encryptionAlgorithmType, Type validationAlgorithmType)
        {
            // Arrange
            var masterKey = "k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==".ToSecret();
            var descriptor = new ManagedAuthenticatedEncryptorDescriptor(new ManagedAuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithmType = encryptionAlgorithmType,
                EncryptionAlgorithmKeySize = 2048,
                ValidationAlgorithmType = validationAlgorithmType
            }, masterKey);

            // Act
            var retVal = descriptor.ExportToXml();

            // Assert
            Assert.Equal(typeof(ManagedAuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
            string expectedXml = string.Format(@"
                <descriptor>
                  <encryption algorithm='{0}' keyLength='2048' />
                  <validation algorithm='{1}' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==</value>
                  </masterKey>
                </descriptor>",
                encryptionAlgorithmType.Name, validationAlgorithmType.Name);
            XmlAssert.Equal(expectedXml, retVal.SerializedDescriptorElement);
        }

        private sealed class MySymmetricAlgorithm : SymmetricAlgorithm
        {
            public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public override void GenerateIV()
            {
                throw new NotImplementedException();
            }

            public override void GenerateKey()
            {
                throw new NotImplementedException();
            }
        }

        private sealed class MyKeyedHashAlgorithm : KeyedHashAlgorithm
        {
            public override void Initialize()
            {
                throw new NotImplementedException();
            }

            protected override void HashCore(byte[] array, int ibStart, int cbSize)
            {
                throw new NotImplementedException();
            }

            protected override byte[] HashFinal()
            {
                throw new NotImplementedException();
            }
        }
    }
}
