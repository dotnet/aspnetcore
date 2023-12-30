// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

public class AuthenticatedEncryptorDescriptorDeserializerTests
{
    [Fact]
    public void ImportFromXml_Cbc_CreatesAppropriateDescriptor()
    {
        // Arrange
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));
        var descriptor = new AuthenticatedEncryptorDescriptor(
            new AuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_192_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
            },
            masterKey.ToSecret());
        var control = CreateEncryptorInstanceFromDescriptor(descriptor);

        var xml = $@"
                <encryptor version='1' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                  <encryption algorithm='AES_192_CBC' />
                  <validation algorithm='HMACSHA512' />
                  <masterKey enc:requiresEncryption='true'>{masterKey}</masterKey>
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
