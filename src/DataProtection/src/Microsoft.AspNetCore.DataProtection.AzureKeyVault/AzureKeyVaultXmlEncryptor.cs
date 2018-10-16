// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Azure.KeyVault.WebKey;

namespace Microsoft.AspNetCore.DataProtection.AzureKeyVault
{
    internal class AzureKeyVaultXmlEncryptor : IXmlEncryptor
    {
        internal static string DefaultKeyEncryption = JsonWebKeyEncryptionAlgorithm.RSAOAEP;
        internal static Func<SymmetricAlgorithm> DefaultSymmetricAlgorithmFactory = Aes.Create;

        private readonly RandomNumberGenerator _randomNumberGenerator;
        private readonly IKeyVaultWrappingClient _client;
        private readonly string _keyId;

        public AzureKeyVaultXmlEncryptor(IKeyVaultWrappingClient client, string keyId)
            : this(client, keyId, RandomNumberGenerator.Create())
        {
        }

        internal AzureKeyVaultXmlEncryptor(IKeyVaultWrappingClient client, string keyId, RandomNumberGenerator randomNumberGenerator)
        {
            _client = client;
            _keyId = keyId;
            _randomNumberGenerator = randomNumberGenerator;
        }

        public EncryptedXmlInfo Encrypt(XElement plaintextElement)
        {
            return EncryptAsync(plaintextElement).GetAwaiter().GetResult();
        }

        private async Task<EncryptedXmlInfo> EncryptAsync(XElement plaintextElement)
        {
            byte[] value;
            using (var memoryStream = new MemoryStream())
            {
                plaintextElement.Save(memoryStream, SaveOptions.DisableFormatting);
                value = memoryStream.ToArray();
            }

            using (var symmetricAlgorithm = DefaultSymmetricAlgorithmFactory())
            {
                var symmetricBlockSize = symmetricAlgorithm.BlockSize / 8;
                var symmetricKey = new byte[symmetricBlockSize];
                var symmetricIV = new byte[symmetricBlockSize];
                _randomNumberGenerator.GetBytes(symmetricKey);
                _randomNumberGenerator.GetBytes(symmetricIV);

                byte[] encryptedValue;
                using (var encryptor = symmetricAlgorithm.CreateEncryptor(symmetricKey, symmetricIV))
                {
                    encryptedValue = encryptor.TransformFinalBlock(value, 0, value.Length);
                }

                var wrappedKey = await _client.WrapKeyAsync(_keyId, DefaultKeyEncryption, symmetricKey);

                var element = new XElement("encryptedKey",
                    new XComment(" This key is encrypted with Azure KeyVault. "),
                    new XElement("kid", wrappedKey.Kid),
                    new XElement("key", Convert.ToBase64String(wrappedKey.Result)),
                    new XElement("iv", Convert.ToBase64String(symmetricIV)),
                    new XElement("value", Convert.ToBase64String(encryptedValue)));

                return new EncryptedXmlInfo(element, typeof(AzureKeyVaultXmlDecryptor));
            }

        }
    }
}
