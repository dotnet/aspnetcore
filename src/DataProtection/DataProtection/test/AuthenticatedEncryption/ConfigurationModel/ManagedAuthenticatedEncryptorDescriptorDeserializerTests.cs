// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

public class ManagedAuthenticatedEncryptorDescriptorDeserializerTests
{
    [Theory]
    [InlineData(typeof(Aes), typeof(HMACSHA1))]
    [InlineData(typeof(Aes), typeof(HMACSHA256))]
    [InlineData(typeof(Aes), typeof(HMACSHA384))]
    [InlineData(typeof(Aes), typeof(HMACSHA512))]
    public void ImportFromXml_BuiltInTypes_CreatesAppropriateDescriptor(Type encryptionAlgorithmType, Type validationAlgorithmType)
    {
        // Arrange
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));
        var descriptor = new ManagedAuthenticatedEncryptorDescriptor(
            new ManagedAuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithmType = encryptionAlgorithmType,
                EncryptionAlgorithmKeySize = 192,
                ValidationAlgorithmType = validationAlgorithmType
            },
            masterKey.ToSecret());
        var control = CreateEncryptorInstanceFromDescriptor(descriptor);

        var xml = $@"
                <descriptor>
                  <encryption algorithm='{encryptionAlgorithmType.Name}' keyLength='192' />
                  <validation algorithm='{validationAlgorithmType.Name}' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>{masterKey}</value>
                  </masterKey>
                </descriptor>";
        var deserializedDescriptor = new ManagedAuthenticatedEncryptorDescriptorDeserializer().ImportFromXml(XElement.Parse(xml));
        var test = CreateEncryptorInstanceFromDescriptor(deserializedDescriptor as ManagedAuthenticatedEncryptorDescriptor);

        // Act & assert
        byte[] plaintext = new byte[] { 1, 2, 3, 4, 5 };
        byte[] aad = new byte[] { 2, 4, 6, 8, 0 };
        byte[] ciphertext = control.Encrypt(new ArraySegment<byte>(plaintext), new ArraySegment<byte>(aad));
        byte[] roundTripPlaintext = test.Decrypt(new ArraySegment<byte>(ciphertext), new ArraySegment<byte>(aad));
        Assert.Equal(plaintext, roundTripPlaintext);
    }

    [Fact]
    public void ImportFromXml_FullyQualifiedBuiltInTypes_CreatesAppropriateDescriptor()
    {
        // Arrange
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));
        var descriptor = new ManagedAuthenticatedEncryptorDescriptor(
            new ManagedAuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithmType = typeof(Aes),
                EncryptionAlgorithmKeySize = 192,
                ValidationAlgorithmType = typeof(HMACSHA384)
            },
            masterKey.ToSecret());
        var control = CreateEncryptorInstanceFromDescriptor(descriptor);

        var xml = $@"
                <descriptor>
                  <encryption algorithm='{typeof(Aes).AssemblyQualifiedName}' keyLength='192' />
                  <validation algorithm='{typeof(HMACSHA384).AssemblyQualifiedName}' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>{masterKey}</value>
                  </masterKey>
                </descriptor>";
        var deserializedDescriptor = new ManagedAuthenticatedEncryptorDescriptorDeserializer().ImportFromXml(XElement.Parse(xml));
        var test = CreateEncryptorInstanceFromDescriptor(deserializedDescriptor as ManagedAuthenticatedEncryptorDescriptor);

        // Act & assert
        byte[] plaintext = new byte[] { 1, 2, 3, 4, 5 };
        byte[] aad = new byte[] { 2, 4, 6, 8, 0 };
        byte[] ciphertext = control.Encrypt(new ArraySegment<byte>(plaintext), new ArraySegment<byte>(aad));
        byte[] roundTripPlaintext = test.Decrypt(new ArraySegment<byte>(ciphertext), new ArraySegment<byte>(aad));
        Assert.Equal(plaintext, roundTripPlaintext);
    }

    [Fact]
    public void ImportFromXml_CustomType_CreatesAppropriateDescriptor()
    {
        // Arrange
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));

        var xml = $@"
                <descriptor>
                  <encryption algorithm='{typeof(CustomAlgorithm).AssemblyQualifiedName}' keyLength='192' />
                  <validation algorithm='{typeof(HMACSHA384).AssemblyQualifiedName}' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>{masterKey}</value>
                  </masterKey>
                </descriptor>";

        // Act
        var deserializedDescriptor = new ManagedAuthenticatedEncryptorDescriptorDeserializer().ImportFromXml(XElement.Parse(xml));
        var managedDescriptor = (ManagedAuthenticatedEncryptorDescriptor)deserializedDescriptor;

        // Assert
        Assert.Equal(typeof(CustomAlgorithm), managedDescriptor.Configuration.EncryptionAlgorithmType);
    }

    [Fact]
    public void ImportFromXml_CustomTypeWithoutConstructor_CreatesAppropriateDescriptor()
    {
        // Arrange
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));

        var xml = $@"
                <descriptor>
                  <encryption algorithm='{typeof(CustomAlgorithmNoConstructor).AssemblyQualifiedName}' keyLength='192' />
                  <validation algorithm='{typeof(HMACSHA384).AssemblyQualifiedName}' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>{masterKey}</value>
                  </masterKey>
                </descriptor>";

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => new ManagedAuthenticatedEncryptorDescriptorDeserializer().ImportFromXml(XElement.Parse(xml)));

        // Assert
        Assert.Equal($"Algorithm type {typeof(CustomAlgorithmNoConstructor).FullName} doesn't have a public parameterless constructor. If the app is published with trimming then the constructor may have been trimmed. Ensure the type's assembly is excluded from trimming.", ex.Message);
    }

    public class CustomAlgorithm : SymmetricAlgorithm
    {
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV) => throw new NotImplementedException();
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV) => throw new NotImplementedException();
        public override void GenerateIV() => throw new NotImplementedException();
        public override void GenerateKey() => throw new NotImplementedException();
    }

    public class CustomAlgorithmNoConstructor : SymmetricAlgorithm
    {
        private CustomAlgorithmNoConstructor() { }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV) => throw new NotImplementedException();
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV) => throw new NotImplementedException();
        public override void GenerateIV() => throw new NotImplementedException();
        public override void GenerateKey() => throw new NotImplementedException();
    }

    private static IAuthenticatedEncryptor CreateEncryptorInstanceFromDescriptor(ManagedAuthenticatedEncryptorDescriptor descriptor)
    {
        var encryptorFactory = new ManagedAuthenticatedEncryptorFactory(NullLoggerFactory.Instance);
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
