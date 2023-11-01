// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

public class CngCbcAuthenticatedEncryptorDescriptorDeserializerTests
{
    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void ImportFromXml_CreatesAppropriateDescriptor()
    {
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));
        // Arrange
        var descriptor = new CngCbcAuthenticatedEncryptorDescriptor(
            new CngCbcAuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithm = Constants.BCRYPT_AES_ALGORITHM,
                EncryptionAlgorithmKeySize = 192,
                EncryptionAlgorithmProvider = null,
                HashAlgorithm = Constants.BCRYPT_SHA512_ALGORITHM,
                HashAlgorithmProvider = null
            },
            masterKey.ToSecret());
        var control = CreateEncryptorInstanceFromDescriptor(descriptor);

        var xml = $@"
                <descriptor version='1' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                  <encryption algorithm='AES' keyLength='192' />
                  <hash algorithm='SHA512' />
                  <masterKey enc:requiresEncryption='true'>{masterKey}</masterKey>
                </descriptor>";
        var deserializedDescriptor = new CngCbcAuthenticatedEncryptorDescriptorDeserializer().ImportFromXml(XElement.Parse(xml));
        var test = CreateEncryptorInstanceFromDescriptor(deserializedDescriptor as CngCbcAuthenticatedEncryptorDescriptor);

        // Act & assert
        byte[] plaintext = new byte[] { 1, 2, 3, 4, 5 };
        byte[] aad = new byte[] { 2, 4, 6, 8, 0 };
        byte[] ciphertext = control.Encrypt(new ArraySegment<byte>(plaintext), new ArraySegment<byte>(aad));
        byte[] roundTripPlaintext = test.Decrypt(new ArraySegment<byte>(ciphertext), new ArraySegment<byte>(aad));
        Assert.Equal(plaintext, roundTripPlaintext);
    }

    private static IAuthenticatedEncryptor CreateEncryptorInstanceFromDescriptor(CngCbcAuthenticatedEncryptorDescriptor descriptor)
    {
        var encryptorFactory = new CngCbcAuthenticatedEncryptorFactory(NullLoggerFactory.Instance);
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
