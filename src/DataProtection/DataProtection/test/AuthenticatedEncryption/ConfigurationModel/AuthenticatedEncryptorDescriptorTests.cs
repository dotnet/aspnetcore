// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Managed;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    public class AuthenticatedEncryptorDescriptorTests
    {
        [ConditionalTheory]
        [ConditionalRunTestOnlyOnWindows]
        [InlineData(EncryptionAlgorithm.AES_128_CBC, ValidationAlgorithm.HMACSHA256)]
        [InlineData(EncryptionAlgorithm.AES_192_CBC, ValidationAlgorithm.HMACSHA256)]
        [InlineData(EncryptionAlgorithm.AES_256_CBC, ValidationAlgorithm.HMACSHA256)]
        [InlineData(EncryptionAlgorithm.AES_128_CBC, ValidationAlgorithm.HMACSHA512)]
        [InlineData(EncryptionAlgorithm.AES_192_CBC, ValidationAlgorithm.HMACSHA512)]
        [InlineData(EncryptionAlgorithm.AES_256_CBC, ValidationAlgorithm.HMACSHA512)]
        public void CreateAuthenticatedEncryptor_RoundTripsData_CngCbcImplementation(EncryptionAlgorithm encryptionAlgorithm, ValidationAlgorithm validationAlgorithm)
        {
            // Parse test input
            int keyLengthInBits = Int32.Parse(Regex.Match(encryptionAlgorithm.ToString(), @"^AES_(?<keyLength>\d{3})_CBC$").Groups["keyLength"].Value, CultureInfo.InvariantCulture);
            string hashAlgorithm = Regex.Match(validationAlgorithm.ToString(), @"^HMAC(?<hashAlgorithm>.*)$").Groups["hashAlgorithm"].Value;

            // Arrange
            var masterKey = Secret.Random(512 / 8);
            var control = new CbcAuthenticatedEncryptor(
                keyDerivationKey: masterKey,
                symmetricAlgorithmHandle: CachedAlgorithmHandles.AES_CBC,
                symmetricAlgorithmKeySizeInBytes: (uint)(keyLengthInBits / 8),
                hmacAlgorithmHandle: BCryptAlgorithmHandle.OpenAlgorithmHandle(hashAlgorithm, hmac: true));
            var test = CreateEncryptorInstanceFromDescriptor(CreateDescriptor(encryptionAlgorithm, validationAlgorithm, masterKey));

            // Act & assert - data round trips properly from control to test
            byte[] plaintext = new byte[] { 1, 2, 3, 4, 5 };
            byte[] aad = new byte[] { 2, 4, 6, 8, 0 };
            byte[] ciphertext = control.Encrypt(new ArraySegment<byte>(plaintext), new ArraySegment<byte>(aad));
            byte[] roundTripPlaintext = test.Decrypt(new ArraySegment<byte>(ciphertext), new ArraySegment<byte>(aad));
            Assert.Equal(plaintext, roundTripPlaintext);
        }

        [ConditionalTheory]
        [ConditionalRunTestOnlyOnWindows]
        [InlineData(EncryptionAlgorithm.AES_128_GCM)]
        [InlineData(EncryptionAlgorithm.AES_192_GCM)]
        [InlineData(EncryptionAlgorithm.AES_256_GCM)]
        public void CreateAuthenticatedEncryptor_RoundTripsData_CngGcmImplementation(EncryptionAlgorithm encryptionAlgorithm)
        {
            // Parse test input
            int keyLengthInBits = Int32.Parse(Regex.Match(encryptionAlgorithm.ToString(), @"^AES_(?<keyLength>\d{3})_GCM$").Groups["keyLength"].Value, CultureInfo.InvariantCulture);

            // Arrange
            var masterKey = Secret.Random(512 / 8);
            var control = new GcmAuthenticatedEncryptor(
                keyDerivationKey: masterKey,
                symmetricAlgorithmHandle: CachedAlgorithmHandles.AES_GCM,
                symmetricAlgorithmKeySizeInBytes: (uint)(keyLengthInBits / 8));
            var test = CreateEncryptorInstanceFromDescriptor(CreateDescriptor(encryptionAlgorithm, ValidationAlgorithm.HMACSHA256 /* unused */, masterKey));

            // Act & assert - data round trips properly from control to test
            byte[] plaintext = new byte[] { 1, 2, 3, 4, 5 };
            byte[] aad = new byte[] { 2, 4, 6, 8, 0 };
            byte[] ciphertext = control.Encrypt(new ArraySegment<byte>(plaintext), new ArraySegment<byte>(aad));
            byte[] roundTripPlaintext = test.Decrypt(new ArraySegment<byte>(ciphertext), new ArraySegment<byte>(aad));
            Assert.Equal(plaintext, roundTripPlaintext);
        }

        public static TheoryData CreateAuthenticatedEncryptor_RoundTripsData_ManagedImplementationData
            => new TheoryData<EncryptionAlgorithm, ValidationAlgorithm, Func<HMAC>>
            {
                { EncryptionAlgorithm.AES_128_CBC, ValidationAlgorithm.HMACSHA256, () => new HMACSHA256() },
                { EncryptionAlgorithm.AES_192_CBC, ValidationAlgorithm.HMACSHA256, () => new HMACSHA256() },
                { EncryptionAlgorithm.AES_256_CBC, ValidationAlgorithm.HMACSHA256, () => new HMACSHA256() },
                { EncryptionAlgorithm.AES_128_CBC, ValidationAlgorithm.HMACSHA512, () => new HMACSHA512() },
                { EncryptionAlgorithm.AES_192_CBC, ValidationAlgorithm.HMACSHA512, () => new HMACSHA512() },
                { EncryptionAlgorithm.AES_256_CBC, ValidationAlgorithm.HMACSHA512, () => new HMACSHA512() },
            };

        [Theory]
        [MemberData(nameof(CreateAuthenticatedEncryptor_RoundTripsData_ManagedImplementationData))]
        public void CreateAuthenticatedEncryptor_RoundTripsData_ManagedImplementation(
            EncryptionAlgorithm encryptionAlgorithm,
            ValidationAlgorithm validationAlgorithm,
            Func<HMAC> validationAlgorithmFactory)
        {
            // Parse test input
            int keyLengthInBits = Int32.Parse(Regex.Match(encryptionAlgorithm.ToString(), @"^AES_(?<keyLength>\d{3})_CBC$").Groups["keyLength"].Value, CultureInfo.InvariantCulture);

            // Arrange
            var masterKey = Secret.Random(512 / 8);
            var control = new ManagedAuthenticatedEncryptor(
                keyDerivationKey: masterKey,
                symmetricAlgorithmFactory: () => Aes.Create(),
                symmetricAlgorithmKeySizeInBytes: keyLengthInBits / 8,
                validationAlgorithmFactory: validationAlgorithmFactory);
            var test = CreateEncryptorInstanceFromDescriptor(CreateDescriptor(encryptionAlgorithm, validationAlgorithm, masterKey));

            // Act & assert - data round trips properly from control to test
            byte[] plaintext = new byte[] { 1, 2, 3, 4, 5 };
            byte[] aad = new byte[] { 2, 4, 6, 8, 0 };
            byte[] ciphertext = control.Encrypt(new ArraySegment<byte>(plaintext), new ArraySegment<byte>(aad));
            byte[] roundTripPlaintext = test.Decrypt(new ArraySegment<byte>(ciphertext), new ArraySegment<byte>(aad));
            Assert.Equal(plaintext, roundTripPlaintext);
        }

        [Fact]
        public void ExportToXml_ProducesCorrectPayload_Cbc()
        {
            // Arrange
            var masterKey = "k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==".ToSecret();
            var descriptor = CreateDescriptor(EncryptionAlgorithm.AES_192_CBC, ValidationAlgorithm.HMACSHA512, masterKey);

            // Act
            var retVal = descriptor.ExportToXml();

            // Assert
            Assert.Equal(typeof(AuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
            const string expectedXml = @"
                <descriptor>
                  <encryption algorithm='AES_192_CBC' />
                  <validation algorithm='HMACSHA512' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==</value>
                  </masterKey>
                </descriptor>";
            XmlAssert.Equal(expectedXml, retVal.SerializedDescriptorElement);
        }

        [Fact]
        public void ExportToXml_ProducesCorrectPayload_Gcm()
        {
            // Arrange
            var masterKey = "k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==".ToSecret();
            var descriptor = CreateDescriptor(EncryptionAlgorithm.AES_192_GCM, ValidationAlgorithm.HMACSHA512, masterKey);

            // Act
            var retVal = descriptor.ExportToXml();

            // Assert
            Assert.Equal(typeof(AuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
            const string expectedXml = @"
                <descriptor>
                  <encryption algorithm='AES_192_GCM' />
                  <!-- some comment here -->
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==</value>
                  </masterKey>
                </descriptor>";
            XmlAssert.Equal(expectedXml, retVal.SerializedDescriptorElement);
        }

        private static AuthenticatedEncryptorDescriptor CreateDescriptor(EncryptionAlgorithm encryptionAlgorithm, ValidationAlgorithm validationAlgorithm, ISecret masterKey)
        {
            return new AuthenticatedEncryptorDescriptor(new AuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithm = encryptionAlgorithm,
                ValidationAlgorithm = validationAlgorithm
            }, masterKey);
        }

        private static IAuthenticatedEncryptor CreateEncryptorInstanceFromDescriptor(AuthenticatedEncryptorDescriptor descriptor)
        {
            var encryptorFactory = new AuthenticatedEncryptorFactory(NullLoggerFactory.Instance);

            // Dummy key with the specified descriptor.
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
