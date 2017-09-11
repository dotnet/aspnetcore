// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.AzureKeyVault.Test
{
    public class AzureKeyVaultXmlEncryptorTests
    {
        [Fact]
        public void UsesKeyVaultToEncryptKey()
        {
            var mock = new Mock<IKeyVaultWrappingClient>();
            mock.Setup(client => client.WrapKeyAsync("key", JsonWebKeyEncryptionAlgorithm.RSAOAEP, It.IsAny<byte[]>()))
                .Returns<string, string, byte[]>((_, __, data) => Task.FromResult(new KeyOperationResult("KeyId", data.Reverse().ToArray())));

            var encryptor = new AzureKeyVaultXmlEncryptor(mock.Object, "key", new MockNumberGenerator());
            var result = encryptor.Encrypt(new XElement("Element"));

            var encryptedElement = result.EncryptedElement;
            var value = encryptedElement.Element("value");

            mock.VerifyAll();
            Assert.NotNull(result);
            Assert.NotNull(value);
            Assert.Equal(typeof(AzureKeyVaultXmlDecryptor), result.DecryptorType);
            Assert.Equal("VfLYL2prdymawfucH3Goso0zkPbQ4/GKqUsj2TRtLzsBPz7p7cL1SQaY6I29xSlsPQf6IjxHSz4sDJ427GvlLQ==", encryptedElement.Element("value").Value);
            Assert.Equal("AAECAwQFBgcICQoLDA0ODw==", encryptedElement.Element("iv").Value);
            Assert.Equal("Dw4NDAsKCQgHBgUEAwIBAA==", encryptedElement.Element("key").Value);
            Assert.Equal("KeyId", encryptedElement.Element("kid").Value);
        }

        [Fact]
        public void UsesKeyVaultToDecryptKey()
        {
            var mock = new Mock<IKeyVaultWrappingClient>();
            mock.Setup(client => client.UnwrapKeyAsync("KeyId", JsonWebKeyEncryptionAlgorithm.RSAOAEP, It.IsAny<byte[]>()))
                .Returns<string, string, byte[]>((_, __, data) => Task.FromResult(new KeyOperationResult(null, data.Reverse().ToArray())))
                .Verifiable();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(mock.Object);

            var encryptor = new AzureKeyVaultXmlDecryptor(serviceCollection.BuildServiceProvider());

            var result = encryptor.Decrypt(XElement.Parse(
                @"<encryptedKey>
                  <kid>KeyId</kid>
                  <key>Dw4NDAsKCQgHBgUEAwIBAA==</key>
                  <iv>AAECAwQFBgcICQoLDA0ODw==</iv>
                  <value>VfLYL2prdymawfucH3Goso0zkPbQ4/GKqUsj2TRtLzsBPz7p7cL1SQaY6I29xSlsPQf6IjxHSz4sDJ427GvlLQ==</value>
                </encryptedKey>"));

            mock.VerifyAll();
            Assert.NotNull(result);
            Assert.Equal("<Element />", result.ToString());
        }

        private class MockNumberGenerator : RandomNumberGenerator
        {
            public override void GetBytes(byte[] data)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (byte)i;
                }
            }
        }
    }
}
