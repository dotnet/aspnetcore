// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    public class AuthenticatedEncryptorDescriptorDeserializerTests
    {
        [Fact]
        public void ImportFromXml_Cbc_CreatesAppropriateDescriptor()
        {
            // Arrange
            var descriptor = new AuthenticatedEncryptorDescriptor(
                new AuthenticatedEncryptorConfiguration()
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_192_CBC,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                },
                "k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==".ToSecret());
            var control = CreateEncryptorInstanceFromDescriptor(descriptor);

            const string xml = @"
                <encryptor version='1' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                  <encryption algorithm='AES_192_CBC' />
                  <validation algorithm='HMACSHA512' />
                  <masterKey enc:requiresEncryption='true'>k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==</masterKey>
                </encryptor>";
            var deserializedDescriptor = new AuthenticatedEncryptorDescriptorDeserializer().ImportFromXml(XElement.Parse(xml));
            var test = CreateEncryptorInstanceFromDescriptor(deserializedDescriptor as AuthenticatedEncryptorDescriptor);

            // Act & assert
            byte[] plaintext = new byte[] { 1, 2, 3, 4, 5 };
            byte[] aad = new byte[] { 2, 4, 6, 8, 0 };
            byte[] ciphertext = control.Encrypt(new ArraySegment<byte>(plaintext), new ArraySegment<byte>(aad));
            byte[] roundTripPlaintext = test.Decrypt(new ArraySegment<byte>(ciphertext), new ArraySegment<byte>(aad));
            Assert.Equal(plaintext, roundTripPlaintext);
        }

        private static IAuthenticatedEncryptor CreateEncryptorInstanceFromDescriptor(AuthenticatedEncryptorDescriptor descriptor)
        {
            var encryptorFactory = new AuthenticatedEncryptorFactory(NullLoggerFactory.Instance);
            var key = new Key(
                Guid.NewGuid(),
                DateTimeOffset.Now,
                DateTimeOffset.Now + TimeSpan.FromHours(1),
                DateTimeOffset.Now + TimeSpan.FromDays(30),
                descriptor,
                new[] { encryptorFactory });


            return key.CreateEncryptor();
        }
    }
}
