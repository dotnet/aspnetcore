// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure.Core.Cryptography;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace Microsoft.AspNetCore.DataProtection.Azure.KeyVault
{
    internal class AzureKeyVaultXmlEncryptor : IXmlEncryptor
    {
        internal static string DefaultKeyEncryption = KeyWrapAlgorithm.RsaOaep.ToString();
        internal static Func<SymmetricAlgorithm> DefaultSymmetricAlgorithmFactory = Aes.Create;

        private readonly RandomNumberGenerator _randomNumberGenerator;
        private readonly IKeyEncryptionKeyResolver _client;
        private readonly string _keyId;

        public AzureKeyVaultXmlEncryptor(IKeyEncryptionKeyResolver client, string keyId)
            : this(client, keyId, RandomNumberGenerator.Create())
        {
        }

        internal AzureKeyVaultXmlEncryptor(IKeyEncryptionKeyResolver client, string keyId,
            RandomNumberGenerator randomNumberGenerator)
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

                var key = await _client.ResolveAsync(_keyId).ConfigureAwait(false);
                var wrappedKey = await key.WrapKeyAsync(DefaultKeyEncryption, symmetricKey);

                var element = new XElement("encryptedKey",
                    new XComment(" This key is encrypted with Azure KeyVault. "),
                    new XElement("kid", key.KeyId),
                    new XElement("key", Convert.ToBase64String(wrappedKey)),
                    new XElement("iv", Convert.ToBase64String(symmetricIV)),
                    new XElement("value", Convert.ToBase64String(encryptedValue)));

                return new EncryptedXmlInfo(element, typeof(AzureKeyVaultXmlDecryptor));
            }

        }
    }
}
