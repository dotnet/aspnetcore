// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    public class AuthenticatedEncryptorDescriptorDeserializerTests
    {
        [Fact]
        public void ImportFromXml_Cbc_CreatesAppropriateDescriptor()
        {
            // Arrange
            var control = new AuthenticatedEncryptorDescriptor(
                new AuthenticatedEncryptionOptions()
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_192_CBC,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                },
                "k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==".ToSecret()).CreateEncryptorInstance();

            const string xml = @"
                <encryptor version='1' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                  <encryption algorithm='AES_192_CBC' />
                  <validation algorithm='HMACSHA512' />
                  <masterKey enc:requiresEncryption='true'>k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==</masterKey>
                </encryptor>";
            var test = new AuthenticatedEncryptorDescriptorDeserializer().ImportFromXml(XElement.Parse(xml)).CreateEncryptorInstance();

            // Act & assert
            byte[] plaintext = new byte[] { 1, 2, 3, 4, 5 };
            byte[] aad = new byte[] { 2, 4, 6, 8, 0 };
            byte[] ciphertext = control.Encrypt(new ArraySegment<byte>(plaintext), new ArraySegment<byte>(aad));
            byte[] roundTripPlaintext = test.Decrypt(new ArraySegment<byte>(ciphertext), new ArraySegment<byte>(aad));
            Assert.Equal(plaintext, roundTripPlaintext);
        }
    }
}
