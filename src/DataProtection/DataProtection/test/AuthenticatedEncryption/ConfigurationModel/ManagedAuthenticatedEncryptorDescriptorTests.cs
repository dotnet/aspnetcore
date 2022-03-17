// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

public class ManagedAuthenticatedEncryptorDescriptorTests
{
    [Fact]
    public void ExportToXml_CustomTypes_ProducesCorrectPayload()
    {
        // Arrange
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));
        var descriptor = new ManagedAuthenticatedEncryptorDescriptor(new ManagedAuthenticatedEncryptorConfiguration()
        {
            EncryptionAlgorithmType = typeof(MySymmetricAlgorithm),
            EncryptionAlgorithmKeySize = 2048,
            ValidationAlgorithmType = typeof(MyKeyedHashAlgorithm)
        }, masterKey.ToSecret());

        // Act
        var retVal = descriptor.ExportToXml();

        // Assert
        Assert.Equal(typeof(ManagedAuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
        var expectedXml = $@"
                <descriptor>
                  <encryption algorithm='{typeof(MySymmetricAlgorithm).AssemblyQualifiedName}' keyLength='2048' />
                  <validation algorithm='{typeof(MyKeyedHashAlgorithm).AssemblyQualifiedName}' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>{masterKey}</value>
                  </masterKey>
                </descriptor>";
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
        var masterKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("[PLACEHOLDER]"));
        var descriptor = new ManagedAuthenticatedEncryptorDescriptor(new ManagedAuthenticatedEncryptorConfiguration()
        {
            EncryptionAlgorithmType = encryptionAlgorithmType,
            EncryptionAlgorithmKeySize = 2048,
            ValidationAlgorithmType = validationAlgorithmType
        }, masterKey.ToSecret());

        // Act
        var retVal = descriptor.ExportToXml();

        // Assert
        Assert.Equal(typeof(ManagedAuthenticatedEncryptorDescriptorDeserializer), retVal.DeserializerType);
        var expectedXml = $@"
                <descriptor>
                  <encryption algorithm='{encryptionAlgorithmType.Name}' keyLength='2048' />
                  <validation algorithm='{validationAlgorithmType.Name}' />
                  <masterKey enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                    <value>{masterKey}</value>
                  </masterKey>
                </descriptor>";
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
